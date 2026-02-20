using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Atomically toggles a social platform published status for a specific part.</summary>
public record TogglePartPlatformCommand(
    Guid PartId,
    SocialPlatform Platform,
    bool IsPublished) : IRequest<bool>;

/// <summary>Handles <see cref="TogglePartPlatformCommand"/>.</summary>
public class TogglePartPlatformCommandHandler(TheContentorDbContext context)
    : IRequestHandler<TogglePartPlatformCommand, bool>
{
    /// <inheritdoc/>
    public async Task<bool> Handle(TogglePartPlatformCommand request, CancellationToken cancellationToken)
    {
        var part = await context.ProcessedPostParts
            .FirstOrDefaultAsync(p => p.Id == request.PartId, cancellationToken);

        if (part == null) return false;

        if (request.IsPublished)
        {
            if (!part.PublishedTo.Contains(request.Platform))
                part.PublishedTo = [.. part.PublishedTo, request.Platform];
        }
        else
        {
            part.PublishedTo = part.PublishedTo.Where(p => p != request.Platform).ToList();
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
