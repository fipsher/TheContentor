import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import tempfile
import aiohttp
import whisper
from azure.servicebus.aio import ServiceBusClient

# Configuration
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus") or os.environ.get("SERVICE_BUS_CONNECTION_STRING")
if not SERVICE_BUS_CONNECTION_STRING:
    raise ValueError("SERVICE_BUS_CONNECTION_STRING is not set")

API_BASE_URL = os.environ.get("TheContentorApiUrl") or os.environ.get("THE_CONTENTOR_API_URL")
if not API_BASE_URL:
    raise ValueError("API_BASE_URL is not set")

COMMANDS_QUEUE_NAME = "subtitle-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"

# Load Whisper model once at startup
print("Loading Whisper model...")
whisper_model = whisper.load_model("base")  # Options: tiny, base, small, medium, large
print("Whisper model loaded successfully")

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
    data.add_field('file',
                   open(file_path, 'rb'),
                   filename=os.path.basename(file_path),
                   content_type='application/x-subrip')

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
    message.application_properties["Type"] = "video-generate-subtitles"
    await sender.send_messages(message)

def format_timestamp(seconds):
    """Format timestamp for SRT format: HH:MM:SS,mmm"""
    hours = int(seconds // 3600)
    minutes = int((seconds % 3600) // 60)
    secs = int(seconds % 60)
    millis = int((seconds % 1) * 1000)
    return f"{hours:02d}:{minutes:02d}:{secs:02d},{millis:03d}"

def generate_srt_with_word_timing(result):
    """Generate SRT file with word-level timing for highlighting effect"""
    srt_content = []
    subtitle_index = 1

    for segment in result['segments']:
        # Whisper provides word-level timestamps in newer versions
        words = segment.get('words', [])

        if words:
            # Create subtitles with word-level timing
            for word_info in words:
                word = word_info.get('word', '').strip()
                start = word_info.get('start', 0)
                end = word_info.get('end', start + 0.5)

                if word:
                    srt_content.append(f"{subtitle_index}")
                    srt_content.append(f"{format_timestamp(start)} --> {format_timestamp(end)}")
                    srt_content.append(word)
                    srt_content.append("")  # Empty line between subtitles
                    subtitle_index += 1
        else:
            # Fall back to segment-level timing if word-level not available
            text = segment['text'].strip()
            start = segment['start']
            end = segment['end']

            if text:
                srt_content.append(f"{subtitle_index}")
                srt_content.append(f"{format_timestamp(start)} --> {format_timestamp(end)}")
                srt_content.append(text)
                srt_content.append("")
                subtitle_index += 1

    return "\n".join(srt_content)

async def generate_subtitles(audio_blob_path, output_path):
    """Generate subtitles from audio using Whisper"""
    with tempfile.TemporaryDirectory() as temp_dir:
        # Download audio
        audio_file = os.path.join(temp_dir, "audio.mp3")
        await download_blob(audio_blob_path['ContainerName'], audio_blob_path['AssetPath'], audio_file)

        # Transcribe with word-level timestamps
        print("Transcribing audio with Whisper...")
        result = whisper_model.transcribe(audio_file, word_timestamps=True)

        # Generate SRT content with word-level timing
        srt_content = generate_srt_with_word_timing(result)

        # Write SRT file
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(srt_content)

        print(f"Subtitles generated: {len(result['segments'])} segments")

async def process_subtitle_command(command, events_sender):
    """Process a single subtitle generation command"""
    try:
        command_type = command.get('CommandType')
        print(f"Processing subtitle command: {command_type} for Part {command.get('PartId')}")

        orchestration_instance_id = command.get("OrchestrationInstanceId")
        processed_post_id = command.get("ProcessedPostId")
        part_id = command.get("PartId")
        audio_blob_path = command.get("AudioBlobPath")

        if not orchestration_instance_id or not audio_blob_path:
            print("Missing required fields")
            return

        try:
            with tempfile.TemporaryDirectory() as temp_dir:
                subtitle_file = os.path.join(temp_dir, f"subtitles_part_{part_id}.srt")

                # Generate subtitles
                await generate_subtitles(audio_blob_path, subtitle_file)

                # Upload to blob storage
                container, blob_path = await upload_to_blob_storage(subtitle_file, "subtitles")

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
                print(f"Successfully generated subtitles for Part {part_id}")

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
            print(f"Failed to generate subtitles: {processing_error}")

    except Exception as e:
        print(f"Error processing subtitle command: {e}")
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

        async with receiver, events_sender:
            print(f"Listening on queue: {COMMANDS_QUEUE_NAME}")
            async for msg in receiver:
                try:
                    body_bytes = b"".join(msg.body)
                    body_str = body_bytes.decode('utf-8')
                    print(f"Received message: {body_str[:100]}...")
                    command = json.loads(body_str)
                    await process_subtitle_command(command, events_sender)
                    await receiver.complete_message(msg)
                except Exception as e:
                    print(f"Error handling message: {type(e).__name__}: {e}")
                    import traceback
                    traceback.print_exc()
                    await receiver.dead_letter_message(msg, reason="ProcessingError", error_description=str(e))

if __name__ == "__main__":
    asyncio.run(main())
