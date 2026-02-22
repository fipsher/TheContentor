import asyncio
import json

from azure.servicebus.aio import ServiceBusClient, AutoLockRenewer
from azure.servicebus.exceptions import MessageLockLostError

from video.config import SERVICE_BUS_CONNECTION_STRING, COMMANDS_QUEUE_NAME, EVENTS_QUEUE_NAME, STORAGE_BASE_PATH
from video.dispatcher import process_video_command


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
