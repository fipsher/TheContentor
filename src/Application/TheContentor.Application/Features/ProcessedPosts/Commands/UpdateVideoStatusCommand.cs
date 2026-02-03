using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Updates video generation status and blob paths for a processed post.</summary>
public record UpdateVideoStatusCommand(
    Guid ProcessedPostId,
    VideoStatus Status,
    Dictionary<Guid, BlobPath> PartVideoBlobPaths) : IRequest;

/// <summary>Applies video status updates from the orchestrator.</summary>
public class UpdateVideoStatusCommandHandler(TheContentorDbContext context)
    : IRequestHandler<UpdateVideoStatusCommand>
{
    /// <summary>Updates the video status and associated blob paths.</summary>
    public async Task Handle(UpdateVideoStatusCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        processedPost.VideoStatus = request.Status;

        // Update part video blob paths
        foreach (var part in processedPost.Parts)
        {
            if (request.PartVideoBlobPaths.TryGetValue(part.Id, out var blobPath))
            {
                part.VideoBlobPath = blobPath;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
