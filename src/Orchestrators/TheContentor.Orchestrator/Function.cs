using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Ryan.Orchestrator.Models;

namespace TheContentor.Orchestrator;

public class Function(ILogger<Function> logger, ServiceBusClient serviceBusClient)
{
    [Function("AssetMetadata")]
    public async Task Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        
    }

    [Function(nameof(SendMessageToAi))]
    public async Task SendMessageToAi([ActivityTrigger] ActivityTrigger<object> trigger)
    {
  
    }

    [Function("EventHandler")]
    public async Task EventHandler(
        [ServiceBusTrigger("events-queue", Connection = "ContentorServiceBus")] ServiceBusReceivedMessage message,
        [DurableClient] DurableTaskClient client)
    {
        var queueMessage = JsonSerializer.Deserialize<object>(message.Body.ToString());
        if (queueMessage == null) throw new ArgumentNullException(nameof(queueMessage));
        
        // await client.RaiseEventAsync(instanceId.ToString()!, "AiCallback", queueMessage.Payload);
  
    }

    [Function("AssetMetadataTrigger")]
    public async Task OrchestratorTriggerer(
        [ServiceBusTrigger("trigger-orchestration", Connection = "ContentorServiceBus")] ServiceBusReceivedMessage message,
        [DurableClient] DurableTaskClient client)
    {
        var queueMessage = JsonSerializer.Deserialize<object>(message.Body.ToString());
        // logger.LogInformation("Triggering orchestration for Id: {Id}", queueMessage?.Id);
        await client.ScheduleNewOrchestrationInstanceAsync("AssetMetadata", queueMessage);
    }
}
