import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import tempfile
import aiohttp
from azure.servicebus.aio import ServiceBusClient, AutoLockRenewer
from azure.servicebus.exceptions import MessageLockLostError
from moviepy.editor import VideoFileClip, concatenate_videoclips, AudioFileClip, CompositeVideoClip, TextClip
from moviepy.video.fx import crop

# Configuration
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus") or os.environ.get("SERVICE_BUS_CONNECTION_STRING")
if not SERVICE_BUS_CONNECTION_STRING:
    raise ValueError("SERVICE_BUS_CONNECTION_STRING is not set")

API_BASE_URL = os.environ.get("TheContentorApiUrl") or os.environ.get("THE_CONTENTOR_API_URL")
if not API_BASE_URL:
    raise ValueError("API_BASE_URL is not set")

COMMANDS_QUEUE_NAME = "video-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"

async def download_blob(container_name, asset_path, output_path):
    """Download blob file from API"""
    url = f"{API_BASE_URL}/api/Blob/download"
    params = {"containerName": container_name, "blobPath": asset_path}

    async with aiohttp.ClientSession() as session:
        async with session.get(url, params=params) as response:
            if response.status not in [200, 201]:
                error_text = await response.text()
                raise Exception(f"Failed to download blob: {response.status} - {error_text}")

            with open(output_path, 'wb') as f:
                f.write(await response.read())

async def upload_to_blob_storage(file_path, container_name):
    """Upload file to API which uploads to Azure Blob Storage"""
    url = f"{API_BASE_URL}/api/Blob/upload"

    data = aiohttp.FormData()
    data.add_field('containerName', container_name)

    # Determine content type based on extension
    ext = os.path.splitext(file_path)[1].lower()
    content_type = 'video/mp4' if ext in ['.mp4', '.mov'] else 'application/octet-stream'

    data.add_field('file',
                   open(file_path, 'rb'),
                   filename=os.path.basename(file_path),
                   content_type=content_type)

    async with aiohttp.ClientSession() as session:
        async with session.post(url, data=data) as response:
            if response.status not in [200, 201]:
                error_text = await response.text()
                raise Exception(f"Failed to upload to API: {response.status} - {error_text}")

            result = await response.json()
            return result.get("containerName"), result.get("assetPath")

async def send_event_callback(sender, callback_data):
    """Send callback event to orchestrator"""
    from azure.servicebus import ServiceBusMessage

    message = ServiceBusMessage(json.dumps(callback_data))
    message.content_type = "application/json"
    if message.application_properties is None:
        message.application_properties = {}
    message.application_properties["Type"] = f"video-{callback_data.get('CommandType', 'unknown')}"
    await sender.send_messages(message)

async def concat_and_cut_video(asset_blob_paths, target_duration, output_path):
    """Concatenate video assets and cut to target duration"""
    with tempfile.TemporaryDirectory() as temp_dir:
        # Download all assets
        asset_files = []
        for i, blob_path in enumerate(asset_blob_paths):
            asset_file = os.path.join(temp_dir, f"asset_{i}.mp4")
            await download_blob(blob_path['ContainerName'], blob_path['AssetPath'], asset_file)
            asset_files.append(asset_file)

        target_seconds = target_duration.total_seconds() if hasattr(target_duration, 'total_seconds') else target_duration

        def _build_and_write_video(files, max_seconds, out_path):
            clips = []
            concatenated = None
            final_clip = None
            try:
                clips = [VideoFileClip(f) for f in files]
                concatenated = concatenate_videoclips(clips, method="compose")
                final_clip = concatenated.subclip(0, min(max_seconds, concatenated.duration))
                final_clip.write_videofile(out_path, codec='libx264', audio=False, fps=30, preset='medium')
                return final_clip.duration
            finally:
                if final_clip is not None:
                    final_clip.close()
                if concatenated is not None:
                    concatenated.close()
                for clip in clips:
                    clip.close()

        return await asyncio.to_thread(_build_and_write_video, asset_files, target_seconds, output_path)

async def compose_final_video(video_blob_path, audio_blob_path, subtitle_blob_path, output_path):
    """Compose final video with audio and subtitles"""
    with tempfile.TemporaryDirectory() as temp_dir:
        # Download files
        video_file = os.path.join(temp_dir, "video.mp4")
        audio_file = os.path.join(temp_dir, "audio.mp3")
        subtitle_file = os.path.join(temp_dir, "subtitles.srt")

        await download_blob(video_blob_path['ContainerName'], video_blob_path['AssetPath'], video_file)
        await download_blob(audio_blob_path['ContainerName'], audio_blob_path['AssetPath'], audio_file)
        await download_blob(subtitle_blob_path['ContainerName'], subtitle_blob_path['AssetPath'], subtitle_file)

        def _build_and_write_final(video_path, audio_path, out_path):
            video_clip = None
            audio_clip = None
            final_clip = None
            try:
                video_clip = VideoFileClip(video_path)
                audio_clip = AudioFileClip(audio_path)
                final_clip = video_clip.set_audio(audio_clip)
                # Add subtitles (MoviePy doesn't have great subtitle support, we'll use ffmpeg directly)
                # For now, write without subtitles and use ffmpeg in next iteration
                # TODO: Implement word-level highlighting with custom subtitle rendering
                final_clip.write_videofile(
                    out_path,
                    codec='libx264',
                    audio_codec='aac',
                    fps=30,
                    preset='medium'
                )
            finally:
                if final_clip is not None:
                    final_clip.close()
                if video_clip is not None:
                    video_clip.close()
                if audio_clip is not None:
                    audio_clip.close()

        await asyncio.to_thread(_build_and_write_final, video_file, audio_file, output_path)

async def process_video_command(command, events_sender):
    """Process a single video command"""
    try:
        command_type = command.get('CommandType')
        print(f"Processing video command: {command_type} for Part {command.get('PartId')}")

        orchestration_instance_id = command.get("OrchestrationInstanceId")
        processed_post_id = command.get("ProcessedPostId")
        part_id = command.get("PartId")

        if not orchestration_instance_id:
            print("Missing orchestration instance ID")
            return

        try:
            if command_type == "concat-cut":
                # Concatenate and cut video
                asset_blob_paths = command.get("AssetBlobPaths", [])
                target_duration_str = command.get("TargetDuration")

                # Parse duration (format: HH:MM:SS or total seconds)
                if isinstance(target_duration_str, str):
                    parts = target_duration_str.split(':')
                    if len(parts) == 3:
                        h, m, s = map(float, parts)
                        target_duration = h * 3600 + m * 60 + s
                    else:
                        target_duration = float(target_duration_str)
                else:
                    target_duration = float(target_duration_str)

                with tempfile.TemporaryDirectory() as temp_dir:
                    output_file = os.path.join(temp_dir, f"video_part_{part_id}.mp4")
                    duration = await concat_and_cut_video(asset_blob_paths, target_duration, output_file)

                    # Upload to blob storage
                    container, blob_path = await upload_to_blob_storage(output_file, "generated-videos")

                    # Send success callback
                    callback = {
                        "OrchestrationInstanceId": orchestration_instance_id,
                        "ProcessedPostId": processed_post_id,
                        "PartId": part_id,
                        "CommandType": command_type,
                        "BlobContainer": container,
                        "BlobPath": blob_path,
                        "Duration": f"{int(duration // 3600):02d}:{int((duration % 3600) // 60):02d}:{duration % 60:06.3f}",
                        "Success": True
                    }
                    await send_event_callback(events_sender, callback)
                    print(f"Successfully processed {command_type} for Part {part_id}")

            elif command_type == "compose":
                # Compose final video with audio and subtitles
                video_blob_path = command.get("VideoBlobPath")
                audio_blob_path = command.get("AudioBlobPath")
                subtitle_blob_path = command.get("SubtitleBlobPath")

                with tempfile.TemporaryDirectory() as temp_dir:
                    output_file = os.path.join(temp_dir, f"final_video_part_{part_id}.mp4")
                    await compose_final_video(video_blob_path, audio_blob_path, subtitle_blob_path, output_file)

                    # Upload to blob storage
                    container, blob_path = await upload_to_blob_storage(output_file, "final-videos")

                    # Send success callback
                    callback = {
                        "OrchestrationInstanceId": orchestration_instance_id,
                        "ProcessedPostId": processed_post_id,
                        "PartId": part_id,
                        "CommandType": command_type,
                        "BlobContainer": container,
                        "BlobPath": blob_path,
                        "Success": True
                    }
                    await send_event_callback(events_sender, callback)
                    print(f"Successfully processed {command_type} for Part {part_id}")

        except Exception as processing_error:
            error_msg = str(processing_error)
            callback = {
                "OrchestrationInstanceId": orchestration_instance_id,
                "ProcessedPostId": processed_post_id,
                "PartId": part_id,
                "CommandType": command_type,
                "BlobContainer": None,
                "BlobPath": None,
                "Success": False,
                "ErrorMessage": error_msg
            }
            await send_event_callback(events_sender, callback)
            print(f"Failed to process video: {processing_error}")

    except Exception as e:
        print(f"Error processing video command: {e}")
        import traceback
        traceback.print_exc()

async def main():
    print(f"Connecting to Service Bus...")
    print(f"Commands Queue: {COMMANDS_QUEUE_NAME}")
    print(f"Events Queue: {EVENTS_QUEUE_NAME}")
    print(f"API Base URL: {API_BASE_URL}")

    client = ServiceBusClient.from_connection_string(SERVICE_BUS_CONNECTION_STRING)
    async with client:
        receiver = client.get_queue_receiver(queue_name=COMMANDS_QUEUE_NAME)
        events_sender = client.get_queue_sender(queue_name=EVENTS_QUEUE_NAME)

        async with receiver, events_sender, AutoLockRenewer(max_lock_renewal_duration=60 * 30) as auto_lock_renewer:
            print(f"Listening on queue: {COMMANDS_QUEUE_NAME}")
            async for msg in receiver:
                auto_lock_renewer.register(receiver, msg, max_lock_renewal_duration=60 * 30)
                try:
                    body_bytes = b"".join(msg.body)
                    body_str = body_bytes.decode('utf-8')
                    print(f"Received message: {body_str[:100]}...")
                    command = json.loads(body_str)
                    await process_video_command(command, events_sender)
                    await receiver.complete_message(msg)
                except Exception as e:
                    print(f"Error handling message: {type(e).__name__}: {e}")
                    import traceback
                    traceback.print_exc()
                    try:
                        await receiver.dead_letter_message(
                            msg,
                            reason="ProcessingError",
                            error_description=str(e),
                        )
                    except MessageLockLostError:
                        print("Message lock lost before dead-letter; skipping settlement.")

if __name__ == "__main__":
    asyncio.run(main())
