using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Stops an in-progress video generation and resets video data.</summary>
public record CancelVideoCommand(Guid ProcessedPostId) : IRequest;

/// <summary>Marks video generation as not generated, clears video data, and signals orchestration termination.</summary>
public class CancelVideoCommandHandler(
    TheContentorDbContext context,
    ServiceBusClient serviceBusClient) : IRequestHandler<CancelVideoCommand>
{
    /// <summary>
    /// Handles the cancellation of video generation for a specified ProcessedPost.
    /// Updates the ProcessedPost and its associated parts to reset video-related data
    /// and notifies the orchestration system of the cancellation event.
    /// </summary>
    /// <param name="request">The cancel video command containing the ProcessedPostId to identify the target ProcessedPost.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>An asynchronous operation.</returns>
    public async Task Handle(CancelVideoCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

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
                Type = "video-cancel",
                ProcessedPostId = request.ProcessedPostId
            }))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "video-cancel",
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
