using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.SourcePosts.Commands;

/// <summary>Resets a post stuck in Processing state back to Raw so it can be re-queued.</summary>
public record ResetAiProcessingCommand(Guid SourcePostId) : IRequest;

/// <summary>Reverts the post status from Processing to Raw.</summary>
public class ResetAiProcessingCommandHandler(TheContentorDbContext context) : IRequestHandler<ResetAiProcessingCommand>
{
    /// <inheritdoc/>
    public async Task Handle(ResetAiProcessingCommand request, CancellationToken cancellationToken)
    {
        var sourcePost = await context.SourcePosts
            .FirstOrDefaultAsync(x => x.Id == request.SourcePostId, cancellationToken);

        if (sourcePost == null)
            throw new InvalidOperationException($"SourcePost {request.SourcePostId} not found.");

        if (sourcePost.Status != SourcePostStatus.Processing)
            throw new InvalidOperationException("Post is not in Processing state.");

        sourcePost.Status = SourcePostStatus.Raw;
        await context.SaveChangesAsync(cancellationToken);
    }
}
