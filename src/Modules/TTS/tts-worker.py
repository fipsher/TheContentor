import asyncio
import json
import os
import aiohttp
import edge_tts
from azure.servicebus.aio import ServiceBusClient

# Configuration from environment variables
SERVICE_BUS_CONNECTION_STRING = os.environ.get("ConnectionStrings__ContentorServiceBus")
QUEUE_NAME = "tts-commands-queue"
# Aspire provides service URLs via environment variables when referenced
API_BASE_URL = os.environ.get("services__the-contentor__http__0") or os.environ.get("API_BASE_URL", "http://localhost:5000")

async def generate_audio(text, voice, output_path):
    communicate = edge_tts.Communicate(text, voice)
    await communicate.save(output_path)

async def upload_asset(session, file_path, filename, tags):
    url = f"{API_BASE_URL}/api/Asset"
    data = aiohttp.FormData()
    data.add_field('File',
                   open(file_path, 'rb'),
                   filename=filename,
                   content_type='audio/mpeg')
    data.add_field('FileName', filename)
    data.add_field('Tags', tags)
    
    async with session.post(url, data=data) as response:
        if response.status >= 400:
            error_text = await response.text()
            print(f"Failed to upload asset {filename}: {response.status} - {error_text}")
        else:
            print(f"Successfully uploaded asset: {filename}")
        return await response.json() if response.status < 400 else None

async def process_message(msg, session):
    try:
        body_str = str(msg)
        print(f"Received message: {body_str}")
        body = json.loads(body_str)
        source_post_id = body.get("SourcePostId")
        if not source_post_id:
            print("No SourcePostId in message")
            return

        print(f"Processing TTS for SourcePost: {source_post_id}")
        
        # Get post data
        async with session.get(f"{API_BASE_URL}/api/SourcePost/{source_post_id}") as response:
            if response.status != 200:
                print(f"Failed to get post data for {source_post_id}: {response.status}")
                return
            post_data = await response.json()
        
        processed_post = post_data.get("processedPost")
        if not processed_post:
            print(f"No processed post data for {source_post_id}")
            return

        gender = processed_post.get("narratorGender") # 1 = Male, 2 = Female
        voice = "en-US-GuyNeural" if gender == 1 else "en-US-MichelleNeural"
        
        temp_dir = f"temp_{source_post_id}"
        os.makedirs(temp_dir, exist_ok=True)
        
        try:
            # Process Description
            description = processed_post.get("description")
            if description:
                path = os.path.join(temp_dir, "description.mp3")
                await generate_audio(description, voice, path)
                await upload_asset(session, path, f"{source_post_id}_description.mp3", "tts")
                
            # Process Parts
            parts = processed_post.get("parts", [])
            for part in parts:
                part_num = part.get("part")
                text = part.get("processedText")
                if text:
                    path = os.path.join(temp_dir, f"part_{part_num}.mp3")
                    await generate_audio(text, voice, path)
                    await upload_asset(session, path, f"{source_post_id}_part_{part_num}.mp3", "tts")
        finally:
            # Cleanup
            if os.path.exists(temp_dir):
                for f in os.listdir(temp_dir):
                    os.remove(os.path.join(temp_dir, f))
                os.rmdir(temp_dir)
        
        print(f"Finished processing TTS for {source_post_id}")

    except Exception as e:
        print(f"Error processing message: {e}")
        import traceback
        traceback.print_exc()

async def main():
    if not SERVICE_BUS_CONNECTION_STRING:
        print("SERVICE_BUS_CONNECTION_STRING not set")
        # For local debugging if not using Aspire env vars
        print("Available env vars:", os.environ.keys())
        return

    print(f"Connecting to Service Bus with connection string starting with: {SERVICE_BUS_CONNECTION_STRING[:20]}...")
    print(f"API Base URL: {API_BASE_URL}")

    async with aiohttp.ClientSession() as session:
        client = ServiceBusClient.from_connection_string(SERVICE_BUS_CONNECTION_STRING)
        async with client:
            receiver = client.get_queue_receiver(queue_name=QUEUE_NAME)
            async with receiver:
                print(f"Listening on queue: {QUEUE_NAME}")
                async for msg in receiver:
                    await process_message(msg, session)
                    await receiver.complete_message(msg)

if __name__ == "__main__":
    asyncio.run(main())
