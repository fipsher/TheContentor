using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TheContentor.Orchestrator.Models.ProcessedPost;
using TheContentor.Orchestrator.Models.TTS;
using TheContentor.Orchestrator.Options;

namespace TheContentor.Orchestrator;

public class Function(ILogger<Function> logger, ServiceBusClient serviceBusClient, IRestClient client, IOptions<ApiOptions> apiOptions)
{
    private readonly string _apiUrl = apiOptions.Value.BaseUrl;
    [Function(nameof(EventHandler))]
    public async Task EventHandler(
        [ServiceBusTrigger("events-queue", Connection = "ContentorServiceBus")] ServiceBusReceivedMessage message,
        [DurableClient] DurableTaskClient client)
    {
        var eventCallback = JsonSerializer.Deserialize<TtsEventCallback>(message.Body.ToString());
        if (eventCallback == null)
        {
            logger.LogWarning("Received null event callback");
            return;
        }

        logger.LogInformation("Raising event TtsCallback for instance {InstanceId}", eventCallback.OrchestrationInstanceId);
        await client.RaiseEventAsync(eventCallback.OrchestrationInstanceId, "TtsCallback", eventCallback);
    }

    [Function(nameof(OrchestratorTriggerer))]
    public async Task OrchestratorTriggerer(
        [ServiceBusTrigger("trigger-orchestration-queue", Connection = "ContentorServiceBus")] ServiceBusReceivedMessage message,
        [DurableClient] DurableTaskClient client)
    {
        var messageType = message.ApplicationProperties.TryGetValue("Type", out var type) ? type?.ToString() : null;

        if (messageType == "tts-generation")
        {
            var ttsRequest = JsonSerializer.Deserialize<TtsOrchestratorRequest>(message.Body.ToString());
            if (ttsRequest != null)
            {
                logger.LogInformation("Triggering TTS orchestration for ProcessedPost: {ProcessedPostId}", ttsRequest.ProcessedPostId);
                await client.ScheduleNewOrchestrationInstanceAsync(nameof(TtsOrchestrator), ttsRequest);
            }
        }
        else
        {
            var queueMessage = JsonSerializer.Deserialize<object>(message.Body.ToString());
            await client.ScheduleNewOrchestrationInstanceAsync("AssetMetadata", queueMessage);
        }
    }

    [Function(nameof(TtsOrchestrator))]
    public async Task TtsOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<TtsOrchestratorRequest>()!;
        var instanceId = context.InstanceId;

        logger.LogInformation("TTS Orchestrator started for ProcessedPost: {ProcessedPostId}, InstanceId: {InstanceId}",
            request.ProcessedPostId, instanceId);

        var state = new TtsOrchestrationState
        {
            ProcessedPostId = request.ProcessedPostId,
            HasErrors = false
        };

        try
        {
            // Fetch processed post data
            var postData = await context.CallActivityAsync<ProcessedPostData>("FetchProcessedPostData", request.ProcessedPostId);

            // Calculate expected callbacks (parts only; description TTS is disabled)
            var expectedCallbacks = postData.Parts.Count;
            state.ExpectedCallbacks = expectedCallbacks;

            // Send TTS commands
            var tasks = new List<Task>();

            // Send part commands
            foreach (var part in postData.Parts)
            {
                // Part 1 narration should include the post subject/title prefix.
                var text = part.Part == 1
                    ? $"{postData.Title} {part.ProcessedText}".Trim()
                    : part.ProcessedText;

                tasks.Add(context.CallActivityAsync("SendTtsCommand", new TtsCommandMessage
                {
                    Text = text,
                    Voice = request.Settings.Voice,
                    Rate = request.Settings.Rate,
                    Pitch = request.Settings.Pitch,
                    ProcessedPostId = request.ProcessedPostId,
                    PartId = part.Id,
                    OrchestrationInstanceId = instanceId,
                    TextType = "part"
                }));
            }

            await Task.WhenAll(tasks);

            // Wait for all callbacks
            while (state.ReceivedCallbacks < state.ExpectedCallbacks)
            {
                var callback = await context.WaitForExternalEvent<TtsEventCallback>("TtsCallback");
                if (callback.TextType != "part")
                {
                    logger.LogInformation("Ignoring TTS callback for {TextType}", callback.TextType);
                    continue;
                }

                state.ReceivedCallbacks++;

                if (callback.Success && !string.IsNullOrEmpty(callback.BlobContainer) && !string.IsNullOrEmpty(callback.BlobPath))
                {
                    var key = $"part-{callback.PartId}";
                    state.CompletedItems[key] = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer,
                        AssetPath = callback.BlobPath,
                        PartId = callback.PartId,
                        TextType = callback.TextType
                    };
                }
                else
                {
                    state.HasErrors = true;
                    logger.LogError("TTS generation failed: {ErrorMessage}", callback.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in TTS Orchestrator for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);
            state.HasErrors = true;
        }
        finally
        {
            // Update ProcessedPost with results (either Success or Failed)
            await context.CallActivityAsync("UpdateProcessedPostTtsStatus", state);
        }

        if (state.HasErrors)
        {
            logger.LogWarning("TTS Orchestrator completed with errors for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);
        }
        else
        {
            logger.LogInformation("TTS Orchestrator completed successfully for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);
        }
    }

    [Function("FetchProcessedPostData")]
    public async Task<ProcessedPostData> FetchProcessedPostData([ActivityTrigger] Guid processedPostId)
    {
        var response = await client.ExecuteGetAsync<ProcessedPostDto>($"{_apiUrl}/api/ProcessedPost/{processedPostId}");

        if (response.Data == null)
        {
            throw new InvalidOperationException($"ProcessedPost not found for ID: {processedPostId}");
        }

        var processedPost = response.Data;

        return new ProcessedPostData
        {
            Title = processedPost.Title,
            Description = processedPost.Description,
            Parts = processedPost.Parts.Select(p => new ProcessedPostPartData
            {
                Id = p.Id!.Value,
                ProcessedText = p.ProcessedText,
                Part = p.Part
            }).ToList()
        };
    }

    [Function("SendTtsCommand")]
    public async Task SendTtsCommand([ActivityTrigger] TtsCommandMessage command)
    {
        var sender = serviceBusClient.CreateSender("commands-topic");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(command))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "tts",
                },
            };

            await sender.SendMessageAsync(message);
            logger.LogInformation("Sent TTS command for {TextType}, PostId: {ProcessedPostId}", command.TextType, command.ProcessedPostId);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    [Function("UpdateProcessedPostTtsStatus")]
    public async Task UpdateProcessedPostTtsStatus([ActivityTrigger] TtsOrchestrationState state)
    {
        var partBlobPaths = state.CompletedItems
            .Where(x => x.Key.StartsWith("part-"))
            .ToDictionary(
                x => x.Value.PartId!.Value,
                x => new { x.Value.ContainerName, x.Value.AssetPath }
            );

        var updatePayload = new
        {
            state.ProcessedPostId,
            Status = state.HasErrors ? 4 : 3, // Failed : Generated
            DescriptionAudioBlobPath = (object?)null,
            PartAudioBlobPaths = partBlobPaths
        };

        var request = new RestRequest($"{_apiUrl}/api/ProcessedPost/tts-status", Method.Put);
        request.AddJsonBody(updatePayload);
        
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to update TTS status: {response.ErrorMessage}");
        }

        logger.LogInformation("Updated TTS status for ProcessedPost: {ProcessedPostId}", state.ProcessedPostId);
    }
}
