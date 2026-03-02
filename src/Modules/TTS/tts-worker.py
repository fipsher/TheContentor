import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import shutil
import tempfile
import uuid
import edge_tts
from mutagen.mp3 import MP3
from azure.servicebus.aio import ServiceBusClient

# Configuration from environment variables
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus") or os.environ.get("SERVICE_BUS_CONNECTION_STRING")

if not SERVICE_BUS_CONNECTION_STRING:
    raise ValueError("SERVICE_BUS_CONNECTION_STRING is not set (checked 'ConnectionStrings__ContentorServiceBus' and 'SERVICE_BUS_CONNECTION_STRING')")

STORAGE_BASE_PATH = os.environ.get("STORAGE_BASE_PATH")
if not STORAGE_BASE_PATH:
    raise ValueError("STORAGE_BASE_PATH is not set")

COMMANDS_QUEUE_NAME = "tts-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"

def save_to_local_storage(file_path, container_name):
    """Save file to local storage, return (containerName, assetPath)"""
    name, ext = os.path.splitext(os.path.basename(file_path))
    unique_name = f"{name}-{uuid.uuid4()}{ext}"
    container_dir = os.path.join(STORAGE_BASE_PATH, container_name)
    os.makedirs(container_dir, exist_ok=True)
    shutil.copy2(file_path, os.path.join(container_dir, unique_name))
    return container_name, unique_name

async def generate_audio_edge_tts(text, voice, rate, pitch, output_path):
    """Generate audio using Edge-TTS with rate and pitch settings"""
    # Edge-TTS rate format: +N% or -N%
    rate_str = f"+{rate}%" if rate >= 0 else f"{rate}%"
    # Edge-TTS pitch format: +NHz or -NHz
    pitch_str = f"+{pitch}Hz" if pitch >= 0 else f"{pitch}Hz"

    communicate = edge_tts.Communicate(text, voice, rate=rate_str, pitch=pitch_str)
    await communicate.save(output_path)

def generate_audio_kokoro(text, voice, rate, output_path):
    """Generate audio using Kokoro-TTS and convert WAV to MP3.

    Rate maps -50..+50 integer to speed multiplier 0.5..1.5.
    Pitch is not supported by Kokoro and is silently ignored.
    """
    import importlib
    import subprocess
    import numpy as np
    import soundfile as sf

    kokoro = importlib.import_module("kokoro")
    KPipeline = getattr(kokoro, "KPipeline")

    # Map rate integer (-50..50) to Kokoro speed float (0.5..1.5)
    speed = max(0.5, min(2.0, 1.0 + rate / 100.0))

    import re

    pipeline = KPipeline(lang_code="a")
    sample_rate = 24000

    # Insert newlines after sentence-ending punctuation so Kokoro's
    # default \n+ split produces natural inter-sentence pauses.
    processed_text = re.sub(r'(?<=[.!?])\s+', '\n', text)

    chunks = []
    for _, _, audio in pipeline(processed_text, voice=voice, speed=speed):
        if audio is not None:
            chunks.append(audio)

    if not chunks:
        raise RuntimeError("Kokoro produced no audio samples")

    wav_path = output_path.replace(".mp3", ".wav")
    sf.write(wav_path, np.concatenate(chunks), sample_rate)
    subprocess.run(["ffmpeg", "-y", "-i", wav_path, "-q:a", "2", output_path],
                   check=True, capture_output=True)
    os.remove(wav_path)

async def generate_audio(text, voice, rate, pitch, engine, output_path):
    """Dispatch to the appropriate TTS engine."""
    if engine == "Kokoro":
        loop = asyncio.get_event_loop()
        await loop.run_in_executor(None, generate_audio_kokoro, text, voice, rate, output_path)
    else:
        await generate_audio_edge_tts(text, voice, rate, pitch, output_path)

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
        engine = command.get("Engine", "EdgeTTS")
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
            print(f"Using TTS engine: {engine}")
            await generate_audio(text, voice, rate, pitch, engine, audio_file)

            # Measure audio duration
            audio_duration_seconds = MP3(audio_file).info.length
            print(f"Audio duration: {audio_duration_seconds:.2f}s")

            # Save to local storage
            container_name = "tts-audio"

            try:
                container, blob_path = save_to_local_storage(audio_file, container_name)

                # Send success callback
                callback = {
                    "OrchestrationInstanceId": orchestration_instance_id,
                    "ProcessedPostId": processed_post_id,
                    "PartId": part_id,
                    "BlobContainer": container,
                    "BlobPath": blob_path,
                    "Success": True,
                    "TextType": text_type,
                    "AudioDurationSeconds": audio_duration_seconds
                }

                await send_event_callback(events_sender, callback)
                print(f"Successfully processed {text_type} for ProcessedPost {processed_post_id}")

            except Exception as upload_error:
                # Send failure callback
                error_msg = str(upload_error)

                callback = {
                    "OrchestrationInstanceId": orchestration_instance_id,
                    "ProcessedPostId": processed_post_id,
                    "PartId": part_id,
                    "BlobContainer": None,
                    "BlobPath": None,
                    "Success": False,
                    "ErrorMessage": error_msg,
                    "TextType": text_type
                }

                await send_event_callback(events_sender, callback)
                print(f"Failed to save audio: {upload_error}")

    except Exception as e:
        print(f"Error processing TTS command: {e}")
        import traceback
        traceback.print_exc()

        # Send failure callback if we have orchestration ID
        if command.get("OrchestrationInstanceId"):
            error_msg = str(e)

            callback = {
                "OrchestrationInstanceId": command.get("OrchestrationInstanceId"),
                "ProcessedPostId": command.get("ProcessedPostId"),
                "PartId": command.get("PartId"),
                "BlobContainer": None,
                "BlobPath": None,
                "Success": False,
                "ErrorMessage": error_msg,
                "TextType": command.get("TextType", "unknown")
            }
            try:
                await send_event_callback(events_sender, callback)
            except:
                pass

async def main():
    print(f"Connecting to Service Bus...")
    # Hide connection string but log its presence
    if SERVICE_BUS_CONNECTION_STRING:
        print(f"Connection string found (length: {len(SERVICE_BUS_CONNECTION_STRING)})")

    print(f"Commands Queue: {COMMANDS_QUEUE_NAME}")
    print(f"Events Queue: {EVENTS_QUEUE_NAME}")
    print(f"Storage Base Path: {STORAGE_BASE_PATH}")

    client = ServiceBusClient.from_connection_string(SERVICE_BUS_CONNECTION_STRING)
    async with client:
        receiver = client.get_queue_receiver(queue_name=COMMANDS_QUEUE_NAME)
        events_sender = client.get_queue_sender(queue_name=EVENTS_QUEUE_NAME)

        async with receiver, events_sender:
            print(f"Listening on queue: {COMMANDS_QUEUE_NAME}")
            async for msg in receiver:
                try:
                    # ServiceBusReceivedMessage.body returns a generator of bytes
                    body_bytes = b"".join(msg.body)
                    body_str = body_bytes.decode('utf-8')
                    print(f"Received message: {body_str[:100]}...")
                    command = json.loads(body_str)
                    await process_tts_command(command, events_sender)
                    await receiver.complete_message(msg)
                except Exception as e:
                    print(f"Error handling message: {type(e).__name__}: {e}")
                    import traceback
                    traceback.print_exc()
                    # Dead letter the message
                    await receiver.dead_letter_message(msg, reason="ProcessingError", error_description=str(e))

if __name__ == "__main__":
    asyncio.run(main())
