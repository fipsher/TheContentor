import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import tempfile
import aiohttp
import edge_tts
from azure.servicebus.aio import ServiceBusClient

# Configuration from environment variables
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus")
COMMANDS_QUEUE_NAME = "tts-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"
BLOB_STORAGE_URL = os.environ.get("ConnectionStrings__blobs") or os.environ.get("BLOB_STORAGE_URL", "http://localhost:10000")

async def generate_audio(text, voice, rate, pitch, output_path):
    """Generate audio using Edge-TTS with rate and pitch settings"""
    # Edge-TTS rate format: +N% or -N%
    rate_str = f"+{rate}%" if rate >= 0 else f"{rate}%"
    # Edge-TTS pitch format: +NHz or -NHz
    pitch_str = f"+{pitch}Hz" if pitch >= 0 else f"{pitch}Hz"

    communicate = edge_tts.Communicate(text, voice, rate=rate_str, pitch=pitch_str)
    await communicate.save(output_path)

async def upload_to_blob_storage(file_path, container_name, blob_name):
    """Upload file to Azure Blob Storage"""
    # This is a simplified version - in production, use Azure SDK
    # For now, we'll store it locally or use the blob emulator
    url = f"{BLOB_STORAGE_URL}/{container_name}/{blob_name}"

    async with aiohttp.ClientSession() as session:
        with open(file_path, 'rb') as f:
            headers = {
                'x-ms-blob-type': 'BlockBlob',
                'Content-Type': 'audio/mpeg'
            }
            async with session.put(url, data=f, headers=headers) as response:
                if response.status not in [200, 201]:
                    error_text = await response.text()
                    raise Exception(f"Failed to upload to blob storage: {response.status} - {error_text}")

    return container_name, blob_name

async def send_event_callback(sender, callback_data):
    """Send callback event to orchestrator"""
    from azure.servicebus import ServiceBusMessage

    message = ServiceBusMessage(json.dumps(callback_data))
    message.content_type = "application/json"
    await sender.send_messages(message)

async def process_tts_command(command, events_sender):
    """Process a single TTS command message"""
    try:
        print(f"Processing TTS command: {command.get('TextType')} for ProcessedPost {command.get('ProcessedPostId')}")

        text = command.get("Text")
        voice = command.get("Voice", "en-US-GuyNeural")
        rate = command.get("Rate", 0)
        pitch = command.get("Pitch", 0)
        processed_post_id = command.get("ProcessedPostId")
        part_id = command.get("PartId")
        orchestration_instance_id = command.get("OrchestrationInstanceId")
        text_type = command.get("TextType", "unknown")

        if not text or not orchestration_instance_id:
            print("Missing required fields in TTS command")
            return

        # Generate audio in temp directory
        with tempfile.TemporaryDirectory() as temp_dir:
            audio_file = os.path.join(temp_dir, f"{text_type}_{processed_post_id}.mp3")
            await generate_audio(text, voice, rate, pitch, audio_file)

            # Upload to blob storage
            container_name = "tts-audio"
            blob_name = f"{processed_post_id}/{text_type}{'_' + str(part_id) if part_id else ''}.mp3"

            try:
                container, blob_path = await upload_to_blob_storage(audio_file, container_name, blob_name)

                # Send success callback
                callback = {
                    "OrchestrationInstanceId": orchestration_instance_id,
                    "ProcessedPostId": processed_post_id,
                    "PartId": part_id,
                    "BlobContainer": container,
                    "BlobPath": blob_path,
                    "Success": True,
                    "TextType": text_type
                }

                await send_event_callback(events_sender, callback)
                print(f"Successfully processed {text_type} for ProcessedPost {processed_post_id}")

            except Exception as upload_error:
                # Send failure callback
                callback = {
                    "OrchestrationInstanceId": orchestration_instance_id,
                    "ProcessedPostId": processed_post_id,
                    "PartId": part_id,
                    "BlobContainer": None,
                    "BlobPath": None,
                    "Success": False,
                    "ErrorMessage": str(upload_error),
                    "TextType": text_type
                }

                await send_event_callback(events_sender, callback)
                print(f"Failed to upload audio: {upload_error}")

    except Exception as e:
        print(f"Error processing TTS command: {e}")
        import traceback
        traceback.print_exc()

        # Send failure callback if we have orchestration ID
        if command.get("OrchestrationInstanceId"):
            callback = {
                "OrchestrationInstanceId": command.get("OrchestrationInstanceId"),
                "ProcessedPostId": command.get("ProcessedPostId"),
                "PartId": command.get("PartId"),
                "BlobContainer": None,
                "BlobPath": None,
                "Success": False,
                "ErrorMessage": str(e),
                "TextType": command.get("TextType", "unknown")
            }
            try:
                await send_event_callback(events_sender, callback)
            except:
                pass

async def main():
    if not SERVICE_BUS_CONNECTION_STRING:
        print("SERVICE_BUS_CONNECTION_STRING not set")
        print("Available env vars:", list(os.environ.keys()))
        return

    print(f"Connecting to Service Bus...")
    print(f"Commands Queue: {COMMANDS_QUEUE_NAME}")
    print(f"Events Queue: {EVENTS_QUEUE_NAME}")
    print(f"Blob Storage URL: {BLOB_STORAGE_URL}")

    client = ServiceBusClient.from_connection_string(SERVICE_BUS_CONNECTION_STRING)
    async with client:
        receiver = client.get_queue_receiver(queue_name=COMMANDS_QUEUE_NAME)
        events_sender = client.get_queue_sender(queue_name=EVENTS_QUEUE_NAME)

        async with receiver, events_sender:
            print(f"Listening on queue: {COMMANDS_QUEUE_NAME}")
            async for msg in receiver:
                try:
                    body_str = str(msg)
                    command = json.loads(body_str)
                    await process_tts_command(command, events_sender)
                    await receiver.complete_message(msg)
                except Exception as e:
                    print(f"Error handling message: {e}")
                    import traceback
                    traceback.print_exc()
                    # Dead letter the message
                    await receiver.dead_letter_message(msg, reason="ProcessingError", error_description=str(e))

if __name__ == "__main__":
    asyncio.run(main())
