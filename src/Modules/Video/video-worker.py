import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import shutil
import tempfile
import uuid
from azure.servicebus.aio import ServiceBusClient, AutoLockRenewer
from azure.servicebus.exceptions import MessageLockLostError
import re
from moviepy.editor import VideoFileClip, concatenate_videoclips, AudioFileClip, CompositeVideoClip, TextClip, ColorClip, ImageClip
import numpy as np
try:
    from PIL import Image, ImageDraw, ImageFont
    PIL_AVAILABLE = True
except Exception:
    PIL_AVAILABLE = False
from moviepy.video.fx import crop

# Configuration
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus") or os.environ.get("SERVICE_BUS_CONNECTION_STRING")
if not SERVICE_BUS_CONNECTION_STRING:
    raise ValueError("SERVICE_BUS_CONNECTION_STRING is not set")

STORAGE_BASE_PATH = os.environ.get("STORAGE_BASE_PATH")
if not STORAGE_BASE_PATH:
    raise ValueError("STORAGE_BASE_PATH is not set")

COMMANDS_QUEUE_NAME = "video-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"

def save_to_local_storage(file_path, container_name):
    """Save file to local storage, return (containerName, assetPath)"""
    name, ext = os.path.splitext(os.path.basename(file_path))
    unique_name = f"{name}-{uuid.uuid4()}{ext}"
    container_dir = os.path.join(STORAGE_BASE_PATH, container_name)
    os.makedirs(container_dir, exist_ok=True)
    shutil.copy2(file_path, os.path.join(container_dir, unique_name))
    return container_name, unique_name

def read_from_local_storage(container_name, asset_path, output_path):
    """Copy file from local storage to output_path"""
    src = os.path.join(STORAGE_BASE_PATH, container_name, asset_path)
    if not os.path.exists(src):
        raise FileNotFoundError(f"Blob not found: {container_name}/{asset_path}")
    shutil.copy2(src, output_path)

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
        # Download all assets from local storage
        asset_files = []
        for i, blob_path in enumerate(asset_blob_paths):
            asset_file = os.path.join(temp_dir, f"asset_{i}.mp4")
            read_from_local_storage(blob_path['ContainerName'], blob_path['AssetPath'], asset_file)
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
        # Read files from local storage
        video_file = os.path.join(temp_dir, "video.mp4")
        audio_file = os.path.join(temp_dir, "audio.mp3")
        subtitle_file = os.path.join(temp_dir, "subtitles.srt")

        read_from_local_storage(video_blob_path['ContainerName'], video_blob_path['AssetPath'], video_file)
        read_from_local_storage(audio_blob_path['ContainerName'], audio_blob_path['AssetPath'], audio_file)
        read_from_local_storage(subtitle_blob_path['ContainerName'], subtitle_blob_path['AssetPath'], subtitle_file)

        def srt_time_to_seconds(time_str):
            """Convert SRT timestamp (HH:MM:SS,mmm) to seconds"""
            match = re.match(r'(\d+):(\d+):(\d+),(\d+)', time_str)
            if not match:
                return 0
            h, m, s, ms = map(int, match.groups())
            return h * 3600 + m * 60 + s + ms / 1000.0

        def parse_srt(srt_path):
            """Parse SRT file into a list of subtitle dictionaries"""
            subtitles = []
            with open(srt_path, 'r', encoding='utf-8') as f:
                content = f.read()

            # Simple SRT regex
            # Handle both \r\n and \n
            content = content.replace('\r\n', '\n')
            pattern = re.compile(r'(\d+)\n(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})\n(.*?)(?=\n\d+\n|\Z)', re.DOTALL)
            for match in pattern.finditer(content):
                index, start, end, text = match.groups()
                subtitles.append({
                    'start': srt_time_to_seconds(start),
                    'end': srt_time_to_seconds(end),
                    'text': text.strip()
                })
            return subtitles

        def _build_and_write_final(video_path, audio_path, subtitle_path, out_path):
            video_clip = None
            audio_clip = None
            final_clip = None
            subtitle_clips = []
            try:
                video_clip = VideoFileClip(video_path)
                audio_clip = AudioFileClip(audio_path)

                # Add subtitles
                subtitles = parse_srt(subtitle_path)

                def _make_pil_subtitle_image(text, video_w, video_h):
                    max_width = int(video_w * 0.9)
                    base_font_size = max(24, int(video_h * 0.055))

                    # Try to find a truetype font; fall back to default if not found
                    font = None
                    if PIL_AVAILABLE:
                        font_candidates = [
                            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                            "/usr/share/fonts/truetype/freefont/FreeSans.ttf",
                            "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
                            "/System/Library/Fonts/Supplemental/Arial.ttf",
                            "/Library/Fonts/Arial.ttf",
                        ]
                        for fp in font_candidates:
                            if os.path.exists(fp):
                                try:
                                    font = ImageFont.truetype(fp, base_font_size)
                                    break
                                except Exception:
                                    continue
                        if font is None:
                            try:
                                font = ImageFont.load_default()
                            except Exception:
                                font = None

                    if not PIL_AVAILABLE or font is None:
                        return None

                    # Prepare for measuring text and wrapping
                    dummy_img = Image.new("RGBA", (max_width, 10), (0, 0, 0, 0))
                    draw = ImageDraw.Draw(dummy_img)

                    # Word-wrap the text to fit the max_width
                    words = text.split()
                    lines = []
                    current = ""
                    for w in words:
                        test = (current + " " + w).strip()
                        bbox = draw.textbbox((0, 0), test, font=font, stroke_width=2)
                        if bbox[2] > max_width and current:
                            lines.append(current)
                            current = w
                        else:
                            current = test
                    if current:
                        lines.append(current)

                    # Compute resulting image size
                    line_spacing = int(base_font_size * 0.2)
                    text_width = 0
                    text_height = 0
                    line_heights = []
                    for ln in lines:
                        bbox = draw.textbbox((0, 0), ln, font=font, stroke_width=2)
                        w = bbox[2] - bbox[0]
                        h = bbox[3] - bbox[1]
                        text_width = max(text_width, w)
                        line_heights.append(h)
                    text_height = sum(line_heights) + (len(lines) - 1) * line_spacing

                    padding_x = 20
                    padding_y = 8
                    img_w = text_width + 2 * padding_x
                    img_h = text_height + 2 * padding_y

                    img = Image.new("RGBA", (img_w, img_h), (0, 0, 0, 0))
                    draw = ImageDraw.Draw(img)

                    # Semi-transparent black box background
                    draw.rectangle([0, 0, img_w, img_h], fill=(0, 0, 0, 160))

                    # Draw each line centered with a thin black stroke
                    y = padding_y
                    for i, ln in enumerate(lines):
                        bbox = draw.textbbox((0, 0), ln, font=font, stroke_width=2)
                        w = bbox[2] - bbox[0]
                        h = bbox[3] - bbox[1]
                        x = (img_w - w) // 2 - bbox[0]
                        draw.text((x, y - bbox[1]), ln, font=font, fill=(255, 255, 255, 255),
                                  stroke_width=2, stroke_fill=(0, 0, 0, 255))
                        y += h + line_spacing

                    return np.array(img)

                for sub in subtitles:
                    start = sub['start']
                    end = sub['end']
                    text = sub['text']

                    made = False

                    # Preferred: PIL-based rendering (no ImageMagick dependency)
                    try:
                        pil_img = _make_pil_subtitle_image(text, video_clip.w, video_clip.h)
                        if pil_img is not None:
                            img_clip = ImageClip(pil_img).set_start(start).set_end(end).set_position(('center', int(video_clip.h * 0.75)))
                            subtitle_clips.append(img_clip)
                            made = True
                    except Exception as e:
                        print(f"Warning: PIL subtitle rendering failed for '{text}': {e}")

                    # Fallback: MoviePy TextClip (may require ImageMagick)
                    if not made:
                        try:
                            txt_clip = TextClip(
                                text,
                                fontsize=max(24, int(video_clip.h * 0.055)),
                                color='white',
                                stroke_color='black',
                                stroke_width=2,
                                method='caption',
                                size=(int(video_clip.w * 0.9), None)
                            ).set_start(start).set_end(end).set_position(('center', int(video_clip.h * 0.75)))
                            subtitle_clips.append(txt_clip)
                            made = True
                        except Exception as e:
                            print(f"Warning: Failed to create TextClip for subtitle '{text}': {e}")

                    if not made:
                        print(f"Warning: Skipped subtitle due to rendering issues: '{text}'")

                if subtitle_clips:
                    final_clip = CompositeVideoClip([video_clip] + subtitle_clips).set_audio(audio_clip)
                else:
                    final_clip = video_clip.set_audio(audio_clip)

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
                for sc in subtitle_clips:
                    sc.close()

        await asyncio.to_thread(_build_and_write_final, video_file, audio_file, subtitle_file, output_path)

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

                    # Save to local storage
                    container, blob_path = save_to_local_storage(output_file, "generated-videos")

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

                    # Save to local storage
                    container, blob_path = save_to_local_storage(output_file, "final-videos")

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
    print(f"Storage Base Path: {STORAGE_BASE_PATH}")

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
