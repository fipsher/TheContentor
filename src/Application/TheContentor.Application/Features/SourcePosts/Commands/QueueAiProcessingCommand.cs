using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.SourcePosts.Commands;

/// <summary>Queues AI processing for a source post without blocking the caller.</summary>
public record QueueAiProcessingCommand(
    Guid SourcePostId,
    int? PartsCount = null,
    int? WordsPerPart = null,
    LlmProvider LlmProvider = LlmProvider.Gemini
) : IRequest;

/// <summary>Sets status to Processing and enqueues the job to Service Bus.</summary>
public class QueueAiProcessingCommandHandler(
    TheContentorDbContext context,
    ServiceBusClient serviceBusClient) : IRequestHandler<QueueAiProcessingCommand>
{
    /// <inheritdoc/>
    public async Task Handle(QueueAiProcessingCommand request, CancellationToken cancellationToken)
    {
        var sourcePost = await context.SourcePosts
            .FirstOrDefaultAsync(x => x.Id == request.SourcePostId, cancellationToken);

        if (sourcePost == null)
            throw new InvalidOperationException($"SourcePost with ID {request.SourcePostId} not found");

        if (sourcePost.Status == SourcePostStatus.Processing)
            throw new InvalidOperationException("AI processing is already in progress for this post");

        sourcePost.Status = SourcePostStatus.Processing;
        await context.SaveChangesAsync(cancellationToken);

        var sender = serviceBusClient.CreateSender("trigger-orchestration-queue");
        try
        {
            var message = new ServiceBusMessage(JsonSerializer.Serialize(new
            {
                SourcePostId = request.SourcePostId,
                request.PartsCount,
                request.WordsPerPart,
                LlmProvider = request.LlmProvider.ToString()
            }))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "ai-processing",
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
