import warnings
warnings.filterwarnings("ignore", message=".*OpenSSL.*")

import asyncio
import json
import os
import shutil
import tempfile
import uuid
import whisper
from azure.servicebus.aio import ServiceBusClient

# Configuration
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus") or os.environ.get("SERVICE_BUS_CONNECTION_STRING")
if not SERVICE_BUS_CONNECTION_STRING:
    raise ValueError("SERVICE_BUS_CONNECTION_STRING is not set")

STORAGE_BASE_PATH = os.environ.get("STORAGE_BASE_PATH")
if not STORAGE_BASE_PATH:
    raise ValueError("STORAGE_BASE_PATH is not set")

COMMANDS_QUEUE_NAME = "subtitle-commands-queue"
EVENTS_QUEUE_NAME = "events-queue"

# Load Whisper model once at startup
print("Loading Whisper model...")
whisper_model = whisper.load_model("base")  # Options: tiny, base, small, medium, large
print("Whisper model loaded successfully")

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
    message.application_properties["Type"] = "video-generate-subtitles"
    await sender.send_messages(message)

def generate_subtitle_json(result, max_phrase_words=5, target_phrase_words=4):
    """Generate JSON subtitle data with phrase grouping and word-level timing.

    Uses Whisper segment boundaries as the primary grouping signal:
    - Segments with > ``max_phrase_words`` words are split into sub-groups of
      ``target_phrase_words`` words.
    - Segments with 1-2 words are merged with the following segment when the
      combined count stays within ``max_phrase_words``.
    - Falls back to segment-level entries when word-level timestamps are
      unavailable.

    Returns a list of phrase dicts:
        [{phrase, start, end, words: [{word, start, end}]}]
    """

    def _clean_segment_words(segment):
        """Extract non-empty word dicts from a Whisper segment."""
        out = []
        for w in segment.get('words', []):
            text = w.get('word', '').strip()
            if text:
                out.append({
                    'word': text,
                    'start': w.get('start', 0),
                    'end': w.get('end', w.get('start', 0) + 0.5),
                })
        return out

    def _make_phrase(word_list):
        return {
            'phrase': ' '.join(w['word'] for w in word_list),
            'start': word_list[0]['start'],
            'end': word_list[-1]['end'],
            'words': word_list,
        }

    # First pass: collect cleaned word lists per segment
    seg_word_lists = []
    for segment in result['segments']:
        cleaned = _clean_segment_words(segment)
        if cleaned:
            seg_word_lists.append(cleaned)
        elif not cleaned and segment.get('text', '').strip():
            # No word-level timing -- keep as single entry
            text = segment['text'].strip()
            seg_word_lists.append([{
                'word': text,
                'start': segment['start'],
                'end': segment['end'],
            }])

    # Second pass: merge short segments (1-2 words) with the next
    merged = []
    carry = []
    for words in seg_word_lists:
        combined = carry + words
        if len(combined) <= max_phrase_words:
            if len(combined) <= 2:
                # Still short -- carry forward to merge with next
                carry = combined
            else:
                merged.append(combined)
                carry = []
        else:
            # Would exceed limit -- flush carry first, then add current
            if carry:
                merged.append(carry)
            carry = words if len(words) <= 2 else []
            if len(words) > 2:
                merged.append(words)
    if carry:
        merged.append(carry)

    # Third pass: split long groups into sub-phrases of target_phrase_words
    phrases = []
    for word_list in merged:
        if len(word_list) <= max_phrase_words:
            phrases.append(_make_phrase(word_list))
        else:
            for i in range(0, len(word_list), target_phrase_words):
                group = word_list[i:i + target_phrase_words]
                if group:
                    phrases.append(_make_phrase(group))

    return phrases

async def generate_subtitles(audio_blob_path, output_path):
    """Generate subtitles from audio using Whisper"""
    with tempfile.TemporaryDirectory() as temp_dir:
        # Read audio from local storage
        audio_file = os.path.join(temp_dir, "audio.mp3")
        read_from_local_storage(audio_blob_path['ContainerName'], audio_blob_path['AssetPath'], audio_file)

        # Transcribe with word-level timestamps
        print("Transcribing audio with Whisper...")
        result = whisper_model.transcribe(audio_file, word_timestamps=True)

        # Generate phrase-grouped JSON with word-level timing
        phrases = generate_subtitle_json(result)

        # Write JSON file
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(phrases, f, ensure_ascii=False, indent=2)

        print(f"Subtitles generated: {len(result['segments'])} segments, {len(phrases)} phrases")

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
                subtitle_file = os.path.join(temp_dir, f"subtitles_part_{part_id}.json")

                # Generate subtitles
                await generate_subtitles(audio_blob_path, subtitle_file)

                # Save to local storage
                container, blob_path = save_to_local_storage(subtitle_file, "subtitles")

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
    print(f"Storage Base Path: {STORAGE_BASE_PATH}")

    client = ServiceBusClient.from_connection_string(SERVICE_BUS_CONNECTION_STRING)
    async with client:
        receiver = client.get_queue_receiver(queue_name=COMMANDS_QUEUE_NAME, prefetch_count=0)
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
