import json

from azure.servicebus import ServiceBusMessage


async def send_event_callback(sender, callback_data):
    """Send callback event to orchestrator"""
    message = ServiceBusMessage(json.dumps(callback_data))
    message.content_type = "application/json"
    if message.application_properties is None:
        message.application_properties = {}
    message.application_properties["Type"] = f"video-{callback_data.get('CommandType', 'unknown')}"
    await sender.send_messages(message)
