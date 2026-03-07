using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TheContentor.Orchestrator.Models.AiProcessing;
using TheContentor.Orchestrator.Models.GenerateAll;
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
        else if (messageType == "ai-processing")
        {
            var aiRequest = JsonSerializer.Deserialize<AiProcessingOrchestratorRequest>(message.Body.ToString());
            if (aiRequest != null)
            {
                logger.LogInformation("Triggering AI processing orchestration for SourcePost: {SourcePostId}", aiRequest.SourcePostId);
                var instanceId = $"ai-processing-{aiRequest.SourcePostId}";
                try
                {
                    await client.ScheduleNewOrchestrationInstanceAsync(
                        nameof(AiProcessingOrchestrator), aiRequest,
                        new StartOrchestrationOptions { InstanceId = instanceId });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to schedule AI processing for SourcePost: {SourcePostId} (may already be running)", aiRequest.SourcePostId);
                }
            }
        }
        else if (messageType == "generate-all")
        {
            var request = JsonSerializer.Deserialize<GenerateAllOrchestratorRequest>(message.Body.ToString());
            if (request != null)
            {
                logger.LogInformation("Triggering GenerateAll orchestration for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);
                var instanceId = $"generate-all-{request.ProcessedPostId}";
                await client.ScheduleNewOrchestrationInstanceAsync(
                    nameof(GenerateAllOrchestrator), request,
                    new StartOrchestrationOptions { InstanceId = instanceId });
            }
        }
        else if (messageType == "generate-all-cancel")
        {
            var cancelRequest = JsonSerializer.Deserialize<VideoCancelRequest>(message.Body.ToString());
            if (cancelRequest != null)
            {
                var instanceId = $"generate-all-{cancelRequest.ProcessedPostId}";
                logger.LogInformation("Terminating GenerateAll orchestration for ProcessedPost: {ProcessedPostId}, InstanceId: {InstanceId}",
                    cancelRequest.ProcessedPostId, instanceId);
                try
                {
                    await client.TerminateInstanceAsync(instanceId, "Canceled by user");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to terminate GenerateAll orchestration for ProcessedPost: {ProcessedPostId}, InstanceId: {InstanceId}",
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
                    Engine = request.Settings.Engine,
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
                        TextType = callback.TextType,
                        AudioDurationSeconds = callback.AudioDurationSeconds
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
                x => new { x.Value.ContainerName, x.Value.AssetPath, x.Value.AudioDurationSeconds }
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

            // Compute sequential video offsets so each part gets the next slice of background video
            var orderedParts = videoData.Parts.OrderBy(p => p.Part).ToList();
            var totalAudioSeconds = orderedParts.Sum(p => p.AudioDuration.TotalSeconds);
            var totalAssetSeconds = videoData.Assets
                .Where(a => a.Duration.HasValue)
                .Sum(a => a.Duration!.Value.TotalSeconds);

            // Pick a deterministic random start (orchestrators must be deterministic)
            var randomStart = TimeSpan.Zero;
            if (totalAssetSeconds > totalAudioSeconds)
            {
                var maxStartSeconds = totalAssetSeconds - totalAudioSeconds;
                var seed = Math.Abs(context.NewGuid().GetHashCode());
                var startSeconds = (seed % (int)(maxStartSeconds * 100)) / 100.0;
                randomStart = TimeSpan.FromSeconds(startSeconds);
            }

            var tasks = new List<Task>();
            var cumulativeOffset = randomStart;

            foreach (var part in orderedParts)
            {
                // Step 1: Concat and cut video to match audio duration, starting at the correct offset
                tasks.Add(context.CallActivityAsync("SendVideoConcatCommand", new VideoCommandMessage
                {
                    CommandType = "concat-cut",
                    ProcessedPostId = request.ProcessedPostId,
                    PartId = part.Id,
                    OrchestrationInstanceId = instanceId,
                    AssetBlobPaths = videoData.Assets.Select(a => a.BlobPath).ToList(),
                    TargetDuration = part.AudioDuration,
                    VideoOffset = cumulativeOffset
                }));

                cumulativeOffset += part.AudioDuration;
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
                    var blobPathInfo = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };
                    partVideos[partId] = blobPathInfo;
                    state.IntermediateVideos[partId] = blobPathInfo;

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
                    var blobPathInfo = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };
                    partSubtitles[partId] = blobPathInfo;
                    state.IntermediateSubtitles[partId] = blobPathInfo;

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
            await context.CallActivityAsync("CleanupIntermediateAssets", state);
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

                var audioDuration = part.AudioDurationSeconds.HasValue
                    ? TimeSpan.FromSeconds(part.AudioDurationSeconds.Value)
                    : TimeSpan.FromSeconds(30);

                parts.Add(new VideoPartData
                {
                    Id = part.Id.Value,
                    Part = part.Part,
                    AudioBlobPath = new BlobPathInfo
                    {
                        ContainerName = part.AudioBlobPath.ContainerName,
                        AssetPath = part.AudioBlobPath.AssetPath
                    },
                    AudioDuration = audioDuration
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

    // ==================== Unified Generate-All Orchestration ====================

    [Function(nameof(GenerateAllOrchestrator))]
    public async Task GenerateAllOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<GenerateAllOrchestratorRequest>()!;
        var instanceId = context.InstanceId;

        logger.LogInformation("GenerateAll Orchestrator started for ProcessedPost: {ProcessedPostId}, InstanceId: {InstanceId}",
            request.ProcessedPostId, instanceId);

        var ttsState = new TtsOrchestrationState
        {
            ProcessedPostId = request.ProcessedPostId,
            HasErrors = false
        };

        try
        {
            // --- Phase 1: TTS ---
            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 0,
                Stage = "TTS", Message = "Starting TTS generation..."
            });

            var postData = await context.CallActivityAsync<ProcessedPostData>("FetchProcessedPostData", request.ProcessedPostId);
            var expectedTtsCallbacks = postData.Parts.Count;
            ttsState.ExpectedCallbacks = expectedTtsCallbacks;

            // Send TTS commands
            var ttsTasks = new List<Task>();
            foreach (var part in postData.Parts)
            {
                var text = part.Part == 1
                    ? $"{postData.Description} {part.ProcessedText}".Trim()
                    : part.ProcessedText;

                ttsTasks.Add(context.CallActivityAsync("SendTtsCommand", new TtsCommandMessage
                {
                    Text = text,
                    Engine = request.TtsSettings.Engine,
                    Voice = request.TtsSettings.Voice,
                    Rate = request.TtsSettings.Rate,
                    Pitch = request.TtsSettings.Pitch,
                    ProcessedPostId = request.ProcessedPostId,
                    PartId = part.Id,
                    OrchestrationInstanceId = instanceId,
                    TextType = "part"
                }));
            }

            await Task.WhenAll(ttsTasks);

            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 5,
                Stage = "TTS", Message = "TTS commands dispatched, waiting for callbacks..."
            });

            // Wait for TTS callbacks
            while (ttsState.ReceivedCallbacks < ttsState.ExpectedCallbacks)
            {
                var callback = await context.WaitForExternalEvent<TtsEventCallback>("TtsCallback");
                if (callback.TextType != "part")
                {
                    logger.LogInformation("Ignoring TTS callback for {TextType}", callback.TextType);
                    continue;
                }

                ttsState.ReceivedCallbacks++;

                if (callback.Success && !string.IsNullOrEmpty(callback.BlobContainer) && !string.IsNullOrEmpty(callback.BlobPath))
                {
                    var key = $"part-{callback.PartId}";
                    ttsState.CompletedItems[key] = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer,
                        AssetPath = callback.BlobPath,
                        PartId = callback.PartId,
                        TextType = callback.TextType,
                        AudioDurationSeconds = callback.AudioDurationSeconds
                    };
                }
                else
                {
                    ttsState.HasErrors = true;
                    logger.LogError("TTS generation failed: {ErrorMessage}", callback.ErrorMessage);
                }

                var ttsPercent = 5 + (int)((ttsState.ReceivedCallbacks / (double)ttsState.ExpectedCallbacks) * 35);
                await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
                {
                    ProcessedPostId = request.ProcessedPostId, ProgressPercent = ttsPercent,
                    Stage = "TTS", Message = $"TTS: {ttsState.ReceivedCallbacks}/{ttsState.ExpectedCallbacks} parts complete"
                });
            }

            // Save TTS results
            await context.CallActivityAsync("UpdateProcessedPostTtsStatus", ttsState);

            if (ttsState.HasErrors)
            {
                await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
                {
                    ProcessedPostId = request.ProcessedPostId, ProgressPercent = 40,
                    Stage = "TTS", Message = "TTS generation failed",
                    IsComplete = true, HasError = true, ErrorMessage = "One or more TTS parts failed"
                });
                return;
            }

            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 40,
                Stage = "TTS", Message = "TTS generation complete"
            });

            // --- Phase 2: Video ---
            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 42,
                Stage = "Video", Message = "Starting video generation..."
            });

            await context.CallActivityAsync("UpdateVideoStatusToInProgress", request.ProcessedPostId);

            var videoData = await context.CallActivityAsync<VideoGenerationData>("FetchVideoGenerationData",
                new FetchVideoGenerationDataInput { ProcessedPostId = request.ProcessedPostId, AssetIds = request.AssetIds });

            var videoState = new VideoOrchestrationState
            {
                ProcessedPostId = request.ProcessedPostId,
                HasErrors = false
            };
            var partsCount = videoData.Parts.Count;
            videoState.ExpectedCallbacks = partsCount * 3;

            // Compute sequential video offsets
            var orderedParts = videoData.Parts.OrderBy(p => p.Part).ToList();
            var totalAudioSeconds = orderedParts.Sum(p => p.AudioDuration.TotalSeconds);
            var totalAssetSeconds = videoData.Assets
                .Where(a => a.Duration.HasValue)
                .Sum(a => a.Duration!.Value.TotalSeconds);

            var randomStart = TimeSpan.Zero;
            if (totalAssetSeconds > totalAudioSeconds)
            {
                var maxStartSeconds = totalAssetSeconds - totalAudioSeconds;
                var seed = Math.Abs(context.NewGuid().GetHashCode());
                var startSeconds = (seed % (int)(maxStartSeconds * 100)) / 100.0;
                randomStart = TimeSpan.FromSeconds(startSeconds);
            }

            var concatTasks = new List<Task>();
            var cumulativeOffset = randomStart;

            foreach (var part in orderedParts)
            {
                concatTasks.Add(context.CallActivityAsync("SendVideoConcatCommand", new VideoCommandMessage
                {
                    CommandType = "concat-cut",
                    ProcessedPostId = request.ProcessedPostId,
                    PartId = part.Id,
                    OrchestrationInstanceId = instanceId,
                    AssetBlobPaths = videoData.Assets.Select(a => a.BlobPath).ToList(),
                    TargetDuration = part.AudioDuration,
                    VideoOffset = cumulativeOffset
                }));

                cumulativeOffset += part.AudioDuration;
            }

            await Task.WhenAll(concatTasks);

            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 45,
                Stage = "Video", Message = "Video concat-cut commands dispatched..."
            });

            // Track per-step callback counts for progress
            var concatCutReceived = 0;
            var subtitleReceived = 0;
            var composeReceived = 0;
            var partVideos = new Dictionary<Guid, BlobPathInfo>();
            var partSubtitles = new Dictionary<Guid, BlobPathInfo>();

            while (videoState.ReceivedCallbacks < videoState.ExpectedCallbacks)
            {
                var callback = await context.WaitForExternalEvent<VideoEventCallback>("VideoCallback");
                videoState.ReceivedCallbacks++;

                if (!callback.Success)
                {
                    videoState.HasErrors = true;
                    logger.LogError("Video processing failed: {ErrorMessage}", callback.ErrorMessage);
                    continue;
                }

                var partId = callback.PartId!.Value;

                if (callback.CommandType == "concat-cut")
                {
                    var blobPathInfo = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };
                    partVideos[partId] = blobPathInfo;
                    videoState.IntermediateVideos[partId] = blobPathInfo;

                    concatCutReceived++;
                    var concatPercent = 45 + (int)((concatCutReceived / (double)partsCount) * 15);
                    await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
                    {
                        ProcessedPostId = request.ProcessedPostId, ProgressPercent = concatPercent,
                        Stage = "Video", Message = $"Concat-cut: {concatCutReceived}/{partsCount} parts"
                    });

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
                    var blobPathInfo = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };
                    partSubtitles[partId] = blobPathInfo;
                    videoState.IntermediateSubtitles[partId] = blobPathInfo;

                    subtitleReceived++;
                    var subtitlePercent = 60 + (int)((subtitleReceived / (double)partsCount) * 15);
                    await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
                    {
                        ProcessedPostId = request.ProcessedPostId, ProgressPercent = subtitlePercent,
                        Stage = "Video", Message = $"Subtitles: {subtitleReceived}/{partsCount} parts"
                    });

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
                    videoState.CompletedItems[key] = new BlobPathInfo
                    {
                        ContainerName = callback.BlobContainer!,
                        AssetPath = callback.BlobPath!,
                        PartId = partId
                    };

                    composeReceived++;
                    var composePercent = 75 + (int)((composeReceived / (double)partsCount) * 20);
                    await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
                    {
                        ProcessedPostId = request.ProcessedPostId, ProgressPercent = composePercent,
                        Stage = "Video", Message = $"Compose: {composeReceived}/{partsCount} parts"
                    });
                }
            }

            // Save video results
            await context.CallActivityAsync("UpdateProcessedPostVideoStatus", videoState);

            if (videoState.HasErrors)
            {
                await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
                {
                    ProcessedPostId = request.ProcessedPostId, ProgressPercent = 95,
                    Stage = "Video", Message = "Video generation completed with errors",
                    IsComplete = true, HasError = true, ErrorMessage = "One or more video parts failed"
                });
                return;
            }

            // --- Phase 3: Cleanup ---
            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 97,
                Stage = "Cleanup", Message = "Cleaning up intermediate files..."
            });

            await context.CallActivityAsync("CleanupIntermediateAssets", videoState);

            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 100,
                Stage = "Complete", Message = "Generation complete!", IsComplete = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in GenerateAll Orchestrator for ProcessedPost: {ProcessedPostId}", request.ProcessedPostId);

            await context.CallActivityAsync("ReportProgress", new GenerationProgressDto
            {
                ProcessedPostId = request.ProcessedPostId, ProgressPercent = 0,
                Stage = "Error", Message = "Pipeline failed unexpectedly",
                IsComplete = true, HasError = true, ErrorMessage = ex.Message
            });
        }
    }

    [Function("ReportProgress")]
    public async Task ReportProgress([ActivityTrigger] GenerationProgressDto progress)
    {
        var request = new RestRequest($"{_apiUrl}/api/ProcessedPost/progress", Method.Post);
        request.AddJsonBody(progress);
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            logger.LogWarning("Failed to report progress: {ErrorMessage}", response.ErrorMessage);
        }
    }

    [Function("UpdateVideoStatusToInProgress")]
    public async Task UpdateVideoStatusToInProgress([ActivityTrigger] Guid processedPostId)
    {
        var request = new RestRequest($"{_apiUrl}/api/ProcessedPost/video-status", Method.Put);
        request.AddJsonBody(new
        {
            ProcessedPostId = processedPostId,
            Status = 2, // InProgress
            PartVideoBlobPaths = new Dictionary<string, object>()
        });
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            logger.LogWarning("Failed to update video status to InProgress: {ErrorMessage}", response.ErrorMessage);
        }
    }

    [Function("CleanupIntermediateAssets")]
    public async Task CleanupIntermediateAssets([ActivityTrigger] VideoOrchestrationState state)
    {
        var additionalBlobPaths = state.IntermediateVideos.Values
            .Concat(state.IntermediateSubtitles.Values)
            .Select(b => new { b.ContainerName, b.AssetPath })
            .ToList();

        var request = new RestRequest($"{_apiUrl}/api/ProcessedPost/{state.ProcessedPostId}/cleanup-intermediate", Method.Post);
        request.AddJsonBody(new { additionalBlobPaths });

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            logger.LogWarning("Failed to cleanup intermediate assets: {ErrorMessage}", response.ErrorMessage);
        }
    }

    // ==================== AI Processing Orchestration ====================

    [Function(nameof(AiProcessingOrchestrator))]
    public async Task AiProcessingOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<AiProcessingOrchestratorRequest>()!;

        logger.LogInformation("AI Processing Orchestrator started for SourcePost: {SourcePostId}", request.SourcePostId);

        try
        {
            await context.CallActivityAsync("RunAiProcessingActivity", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI Processing Orchestrator failed for SourcePost: {SourcePostId}", request.SourcePostId);
            await context.CallActivityAsync("ReportAiProcessingStatus", new AiProcessingStatusPayload
            {
                SourcePostId = request.SourcePostId,
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [Function("RunAiProcessingActivity")]
    public async Task RunAiProcessingActivity([ActivityTrigger] AiProcessingOrchestratorRequest request)
    {
        logger.LogInformation("Running AI processing for SourcePost: {SourcePostId}", request.SourcePostId);

        var processRequest = new RestRequest($"{_apiUrl}/api/SourcePost/{request.SourcePostId}/process-ai-internal", Method.Post);
        processRequest.AddJsonBody(new
        {
            request.PartsCount,
            request.WordsPerPart,
            request.LlmProvider
        });

        var processResponse = await client.ExecuteAsync(processRequest);
        if (!processResponse.IsSuccessful)
            throw new Exception($"AI processing failed: {processResponse.Content ?? processResponse.ErrorMessage}");

        // Report success
        await ReportAiProcessingStatus(new AiProcessingStatusPayload
        {
            SourcePostId = request.SourcePostId,
            Success = true
        });
    }

    [Function("ReportAiProcessingStatus")]
    public async Task ReportAiProcessingStatus([ActivityTrigger] AiProcessingStatusPayload payload)
    {
        var statusRequest = new RestRequest($"{_apiUrl}/api/SourcePost/{payload.SourcePostId}/ai-status", Method.Post);
        statusRequest.AddJsonBody(new
        {
            payload.Success,
            payload.ErrorMessage
        });

        var response = await client.ExecuteAsync(statusRequest);
        if (!response.IsSuccessful)
            logger.LogWarning("Failed to report AI processing status for SourcePost: {SourcePostId}: {Error}", payload.SourcePostId, response.ErrorMessage);
    }
}

/// <summary>Payload for reporting AI processing completion status.</summary>
public class AiProcessingStatusPayload
{
    /// <summary>Source post identifier.</summary>
    public Guid SourcePostId { get; set; }
    /// <summary>Whether processing succeeded.</summary>
    public bool Success { get; set; }
    /// <summary>Error message on failure.</summary>
    public string? ErrorMessage { get; set; }
}
