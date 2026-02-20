using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Triggers the full TTS + Video generation pipeline.</summary>
public record GenerateAllCommand(
    Guid ProcessedPostId,
    TtsSettingsModel TtsSettings,
    List<Guid> AssetIds) : IRequest;

/// <summary>Cleans up previous assets, updates status, and enqueues the unified pipeline.</summary>
public class GenerateAllCommandHandler(
    TheContentorDbContext context,
    IMediator mediator,
    ServiceBusClient serviceBusClient) : IRequestHandler<GenerateAllCommand>
{
    /// <summary>Handles the generate-all command.</summary>
    public async Task Handle(GenerateAllCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        if (processedPost.TtsStatus == TtsStatus.InProgress || processedPost.VideoStatus == VideoStatus.InProgress)
        {
            throw new InvalidOperationException("Generation is already in progress");
        }

        // Clean up previous assets if this is a re-run
        if (processedPost.TtsStatus == TtsStatus.Generated || processedPost.VideoStatus == VideoStatus.Generated)
        {
            await mediator.Send(new CleanupPreviousAssetsCommand(request.ProcessedPostId), cancellationToken);
        }

        // Reload after cleanup may have changed the entity
        await context.Entry(processedPost).ReloadAsync(cancellationToken);
        foreach (var part in processedPost.Parts)
        {
            await context.Entry(part).ReloadAsync(cancellationToken);
        }

        // Update status and settings
        processedPost.TtsStatus = TtsStatus.InProgress;
        processedPost.VideoStatus = VideoStatus.NotGenerated;
        processedPost.TtsSettings = JsonSerializer.Serialize(request.TtsSettings);
        var videoSettings = new VideoSettingsModel { AssetIds = request.AssetIds };
        processedPost.VideoSettings = JsonSerializer.Serialize(videoSettings);
        await context.SaveChangesAsync(cancellationToken);

        // Send message to orchestration trigger queue
        var sender = serviceBusClient.CreateSender("trigger-orchestration-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(new
            {
                Type = "generate-all",
                ProcessedPostId = request.ProcessedPostId,
                TtsSettings = request.TtsSettings,
                AssetIds = request.AssetIds
            }))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "generate-all",
                },
            };

            await sender.SendMessageAsync(message, cancellationToken);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}
