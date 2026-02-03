using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Triggers text-to-speech generation for a processed post.</summary>
public record GenerateTtsCommand(Guid ProcessedPostId, TtsSettingsModel Settings) : IRequest;

/// <summary>Queues a TTS orchestration message and updates status.</summary>
public class GenerateTtsCommandHandler(
    TheContentorDbContext context,
    ServiceBusClient serviceBusClient) : IRequestHandler<GenerateTtsCommand>
{
    /// <summary>Marks the post as in progress and enqueues the TTS job.</summary>
    public async Task Handle(GenerateTtsCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        // Update status and settings
        processedPost.TtsStatus = TtsStatus.InProgress;
        processedPost.TtsSettings = JsonSerializer.Serialize(request.Settings);
        await context.SaveChangesAsync(cancellationToken);

        // Send message to orchestration trigger queue
        var sender = serviceBusClient.CreateSender("trigger-orchestration-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(new
            {
                Type = "tts-generation",
                ProcessedPostId = request.ProcessedPostId,
                Settings = request.Settings
            }))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "tts-generation",
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
