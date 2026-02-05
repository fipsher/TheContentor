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
using TheContentor.Orchestrator.Models.Video;
using TheContentor.Orchestrator.Options;

namespace TheContentor.Orchestrator;

public class Function(ILogger<Function> logger, ServiceBusClient serviceBusClient, IRestClient client, IOptions<ApiOptions> apiOptions)
{
    private readonly string _apiUrl = apiOptions.Value.BaseUrl;
    private static string GetVideoInstanceId(Guid processedPostId) => $"video-{processedPostId}";

    [Function(nameof(EventHandler))]
    public async Task EventHandler(
        [ServiceBusTrigger("events-queue", Connection = "ContentorServiceBus")] ServiceBusReceivedMessage message,
        [DurableClient] DurableTaskClient client)
    {
        var messageBody = message.Body.ToString();
        var messageType = message.ApplicationProperties.TryGetValue("Type", out var type) ? type?.ToString() : null;

        // Try to parse as video event first if type suggests it
        if (messageType?.StartsWith("video") == true)
        {
            var videoCallback = JsonSerializer.Deserialize<VideoEventCallback>(messageBody);
            if (videoCallback != null)
            {
                logger.LogInformation("Raising event VideoCallback for instance {InstanceId}", videoCallback.OrchestrationInstanceId);
                await client.RaiseEventAsync(videoCallback.OrchestrationInstanceId, "VideoCallback", videoCallback);
                return;
            }
        }

        // Otherwise try TTS callback
        var eventCallback = JsonSerializer.Deserialize<TtsEventCallback>(messageBody);
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
        else if (messageType == "video-generation")
        {
            var videoRequest = JsonSerializer.Deserialize<VideoOrchestratorRequest>(message.Body.ToString());
            if (videoRequest != null)
            {
                logger.LogInformation("Triggering Video orchestration for ProcessedPost: {ProcessedPostId}", videoRequest.ProcessedPostId);
                var instanceId = GetVideoInstanceId(videoRequest.ProcessedPostId);
                await client.ScheduleNewOrchestrationInstanceAsync(
                    nameof(VideoOrchestrator),
                    videoRequest,
                    new StartOrchestrationOptions { InstanceId = instanceId });
            }
        }
        else if (messageType == "video-cancel")
        {
            var cancelRequest = JsonSerializer.Deserialize<VideoCancelRequest>(message.Body.ToString());
            if (cancelRequest != null)
            {
                var instanceId = GetVideoInstanceId(cancelRequest.ProcessedPostId);
                logger.LogInformation("Terminating Video orchestration for ProcessedPost: {ProcessedPostId}, InstanceId: {InstanceId}",
                    cancelRequest.ProcessedPostId, instanceId);
                try
                {
                    await client.TerminateInstanceAsync(instanceId, "Canceled by user");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to terminate Video orchestration for ProcessedPost: {ProcessedPostId}, InstanceId: {InstanceId}",
                        cancelRequest.ProcessedPostId, instanceId);
                }
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

    // ==================== Video Orchestration ====================

    [Function(nameof(VideoOrchestrator))]
    public async Task VideoOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<VideoOrchestratorRequest>()!;
        var instanceId = context.InstanceId;

        logger.LogInformation("Video Orchestrator started for ProcessedPost: {ProcessedPostId}, InstanceId: {InstanceId}",
            request.ProcessedPostId, instanceId);

        var state = new VideoOrchestrationState
        {
            ProcessedPostId = request.ProcessedPostId,
            HasErrors = false
        };

        try
        {
            // Fetch video generation data (parts with audio, assets)
            var videoData = await context.CallActivityAsync<VideoGenerationData>("FetchVideoGenerationData",
                new FetchVideoGenerationDataInput { ProcessedPostId = request.ProcessedPostId, AssetIds = request.Settings.AssetIds });

            // For each part: concat/cut video -> generate subtitles -> compose final video
            // Expected callbacks: (concat-cut + subtitles + compose) * parts.Count
            state.ExpectedCallbacks = videoData.Parts.Count * 3;

            var tasks = new List<Task>();

            foreach (var part in videoData.Parts)
            {
                // Step 1: Concat and cut video to match audio duration
                tasks.Add(context.CallActivityAsync("SendVideoConcatCommand", new VideoCommandMessage
                {
                    CommandType = "concat-cut",
                    ProcessedPostId = request.ProcessedPostId,
                    PartId = part.Id,
                    OrchestrationInstanceId = instanceId,
                    AssetBlobPaths = videoData.Assets.Select(a => a.BlobPath).ToList(),
                    TargetDuration = part.AudioDuration
                }));
            }

            await Task.WhenAll(tasks);

            // Process callbacks sequentially: concat-cut -> subtitles -> compose for each part
            var partVideos = new Dictionary<Guid, BlobPathInfo>();
            var partSubtitles = new Dictionary<Guid, BlobPathInfo>();

            while (state.ReceivedCallbacks < state.ExpectedCallbacks)
            {
                var callback = await context.WaitForExternalEvent<VideoEventCallback>("VideoCallback");
                state.ReceivedCallbacks++;

                if (!callback.Success)
                {
                    state.HasErrors = true;
                    logger.LogError("Video processing failed: {ErrorMessage}", callback.ErrorMessage);
                    continue;
                }

                var partId = callback.PartId!.Value;

                if (callback.CommandType == "concat-cut")
                {
                    partVideos[partId] = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };

                    // Trigger subtitle generation
                    var part = videoData.Parts.First(p => p.Id == partId);
                    await context.CallActivityAsync("SendSubtitleGenerationCommand", new VideoCommandMessage
                    {
                        CommandType = "generate-subtitles",
                        ProcessedPostId = request.ProcessedPostId,
                        PartId = partId,
                        OrchestrationInstanceId = instanceId,
                        AudioBlobPath = part.AudioBlobPath
                    });
                }
                else if (callback.CommandType == "generate-subtitles")
                {
                    partSubtitles[partId] = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };

                    // Trigger video composition
                    await context.CallActivityAsync("SendVideoComposeCommand", new VideoCommandMessage
                    {
                        CommandType = "compose",
                        ProcessedPostId = request.ProcessedPostId,
                        PartId = partId,
                        OrchestrationInstanceId = instanceId,
                        VideoBlobPath = partVideos[partId],
                        SubtitleBlobPath = partSubtitles[partId],
                        AudioBlobPath = videoData.Parts.First(p => p.Id == partId).AudioBlobPath
                    });
                }
                else if (callback.CommandType == "compose")
                {
                    var key = $"part-{partId}";
                    state.CompletedItems[key] = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in Video Orchestrator for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);
            state.HasErrors = true;
        }
        finally
        {
            await context.CallActivityAsync("UpdateProcessedPostVideoStatus", state);
        }

        if (state.HasErrors)
        {
            logger.LogWarning("Video Orchestrator completed with errors for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);
        }
        else
        {
            logger.LogInformation("Video Orchestrator completed successfully for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);
        }
    }

    [Function("FetchVideoGenerationData")]
    public async Task<VideoGenerationData> FetchVideoGenerationData([ActivityTrigger] FetchVideoGenerationDataInput input)
    {
        Guid processedPostId = input.ProcessedPostId;
        List<Guid> assetIds = input.AssetIds;

        var response = await client.ExecuteGetAsync<ProcessedPostDetailsResponse>($"{_apiUrl}/api/ProcessedPost/{processedPostId}");
        if (response.Data == null)
        {
            throw new InvalidOperationException($"ProcessedPost not found for ID: {processedPostId}");
        }

        var processedPost = response.Data;

        // Map parts from API response
        var parts = new List<VideoPartData>();
        foreach (var part in processedPost.Parts)
        {
            if (part.AudioBlobPath != null)
            {
                if (!part.Id.HasValue)
                {
                    throw new InvalidOperationException($"ProcessedPost part is missing an ID for ProcessedPost: {processedPostId}");
                }

                parts.Add(new VideoPartData
                {
                    Id = part.Id.Value,
                    Part = part.Part,
                    AudioBlobPath = new BlobPathInfo
                    {
                        ContainerName = part.AudioBlobPath.ContainerName,
                        AssetPath = part.AudioBlobPath.AssetPath
                    },
                    AudioDuration = TimeSpan.FromSeconds(30) // TODO: Get actual duration from blob metadata or DB
                });
            }
        }

        // Fetch selected assets
        var assets = new List<AssetData>();
        foreach (var assetId in assetIds)
        {
            var assetResponse = await client.ExecuteGetAsync<AssetDetailsResponse>($"{_apiUrl}/api/Asset/{assetId}");
            if (assetResponse.Data != null)
            {
                var asset = assetResponse.Data;
                if (asset.BlobPath == null)
                {
                    throw new InvalidOperationException($"Asset blob path missing for Asset ID: {assetId}");
                }

                assets.Add(new AssetData
                {
                    Id = assetId,
                    BlobPath = new BlobPathInfo
                    {
                        ContainerName = asset.BlobPath.ContainerName,
                        AssetPath = asset.BlobPath.AssetPath
                    },
                    Duration = asset.Duration != null ? TimeSpan.Parse(asset.Duration) : null
                });
            }
        }

        return new VideoGenerationData
        {
            Parts = parts,
            Assets = assets
        };
    }

    [Function("SendVideoConcatCommand")]
    public async Task SendVideoConcatCommand([ActivityTrigger] VideoCommandMessage command)
    {
        var sender = serviceBusClient.CreateSender("video-commands-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(command))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "video-concat-cut",
                },
            };

            await sender.SendMessageAsync(message);
            logger.LogInformation("Sent video concat-cut command for Part: {PartId}", command.PartId);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    [Function("SendSubtitleGenerationCommand")]
    public async Task SendSubtitleGenerationCommand([ActivityTrigger] VideoCommandMessage command)
    {
        var sender = serviceBusClient.CreateSender("subtitle-commands-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(command))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "subtitle-generation",
                },
            };

            await sender.SendMessageAsync(message);
            logger.LogInformation("Sent subtitle generation command for Part: {PartId}", command.PartId);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    [Function("SendVideoComposeCommand")]
    public async Task SendVideoComposeCommand([ActivityTrigger] VideoCommandMessage command)
    {
        var sender = serviceBusClient.CreateSender("video-commands-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(command))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "video-compose",
                },
            };

            await sender.SendMessageAsync(message);
            logger.LogInformation("Sent video compose command for Part: {PartId}", command.PartId);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    [Function("UpdateProcessedPostVideoStatus")]
    public async Task UpdateProcessedPostVideoStatus([ActivityTrigger] VideoOrchestrationState state)
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
            PartVideoBlobPaths = partBlobPaths
        };

        var request = new RestRequest($"{_apiUrl}/api/ProcessedPost/video-status", Method.Put);
        request.AddJsonBody(updatePayload);

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            throw new Exception($"Failed to update video status: {response.ErrorMessage}");
        }

        logger.LogInformation("Updated video status for ProcessedPost: {ProcessedPostId}", state.ProcessedPostId);
    }
}
