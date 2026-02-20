using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Sets the posted status for a processed post.</summary>
public record MarkProcessedPostAsPostedCommand(Guid ProcessedPostId, bool IsPosted) : IRequest<bool>;

/// <summary>Handles <see cref="MarkProcessedPostAsPostedCommand"/>.</summary>
public class MarkProcessedPostAsPostedCommandHandler(TheContentorDbContext context)
    : IRequestHandler<MarkProcessedPostAsPostedCommand, bool>
{
    /// <inheritdoc/>
    public async Task<bool> Handle(MarkProcessedPostAsPostedCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ProcessedPosts
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (entity == null) return false;

        entity.IsPosted = request.IsPosted;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
