using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Cancels an in-progress generate-all pipeline.</summary>
public record CancelGenerationCommand(Guid ProcessedPostId) : IRequest;

/// <summary>Resets generation status and signals orchestration termination.</summary>
public class CancelGenerationCommandHandler(
    TheContentorDbContext context,
    ServiceBusClient serviceBusClient) : IRequestHandler<CancelGenerationCommand>
{
    /// <summary>Handles cancellation of the unified generation pipeline.</summary>
    public async Task Handle(CancelGenerationCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        processedPost.TtsStatus = TtsStatus.NotGenerated;
        processedPost.VideoStatus = VideoStatus.NotGenerated;
        processedPost.VideoSettings = null;
        processedPost.VideoBlobPath = null;

        foreach (var part in processedPost.Parts)
        {
            part.VideoBlobPath = null;
            part.SubtitleBlobPath = null;
        }

        await context.SaveChangesAsync(cancellationToken);

        var sender = serviceBusClient.CreateSender("trigger-orchestration-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(new
            {
                Type = "generate-all-cancel",
                ProcessedPostId = request.ProcessedPostId
            }))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "generate-all-cancel",
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
