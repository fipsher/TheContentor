using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Triggers video generation for a processed post.</summary>
public record GenerateVideoCommand(Guid ProcessedPostId, List<Guid> AssetIds) : IRequest;

/// <summary>Queues a video generation orchestration message and updates status.</summary>
public class GenerateVideoCommandHandler(
    TheContentorDbContext context,
    ServiceBusClient serviceBusClient) : IRequestHandler<GenerateVideoCommand>
{
    /// <summary>Marks the post as in progress and enqueues the video generation job.</summary>
    public async Task Handle(GenerateVideoCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        // Ensure TTS has been generated
        if (processedPost.TtsStatus != TtsStatus.Generated)
        {
            throw new InvalidOperationException("TTS must be generated before creating video");
        }

        // Update status and settings
        processedPost.VideoStatus = VideoStatus.InProgress;
        var settings = new VideoSettingsModel { AssetIds = request.AssetIds };
        processedPost.VideoSettings = JsonSerializer.Serialize(settings);

        // Track last usage time on selected assets
        var usedAssets = await context.Assets
            .Where(a => request.AssetIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var asset in usedAssets)
            asset.LastUsedAt = now;

        await context.SaveChangesAsync(cancellationToken);

        // Send message to orchestration trigger queue
        var sender = serviceBusClient.CreateSender("trigger-orchestration-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(new
            {
                Type = "video-generation",
                ProcessedPostId = request.ProcessedPostId,
                Settings = settings
            }))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "video-generation",
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
