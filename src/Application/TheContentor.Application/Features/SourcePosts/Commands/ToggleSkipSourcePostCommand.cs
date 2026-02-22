using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.SourcePosts.Commands;

/// <summary>Toggles the skipped status of a source post.</summary>
public record ToggleSkipSourcePostCommand(Guid SourcePostId) : IRequest;

/// <summary>Marks the post as skipped, or restores it to its previous state if already skipped.</summary>
public class ToggleSkipSourcePostCommandHandler(TheContentorDbContext context)
    : IRequestHandler<ToggleSkipSourcePostCommand>
{
    /// <summary>Flips the post between <see cref="SourcePostStatus.Skipped"/> and <see cref="SourcePostStatus.Raw"/>.</summary>
    public async Task Handle(ToggleSkipSourcePostCommand request, CancellationToken cancellationToken)
    {
        var post = await context.SourcePosts
            .FirstOrDefaultAsync(x => x.Id == request.SourcePostId, cancellationToken);

        if (post == null) return;

        post.Status = post.Status == SourcePostStatus.Skipped
            ? SourcePostStatus.Raw
            : SourcePostStatus.Skipped;

        await context.SaveChangesAsync(cancellationToken);
    }
}
