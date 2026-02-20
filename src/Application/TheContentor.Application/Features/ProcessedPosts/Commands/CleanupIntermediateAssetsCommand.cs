using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Deletes intermediate pipeline assets after successful generation.</summary>
public record CleanupIntermediateAssetsCommand(Guid ProcessedPostId) : IRequest;

/// <summary>Removes subtitle files that are no longer needed after video composition.</summary>
public class CleanupIntermediateAssetsCommandHandler(
    TheContentorDbContext context,
    IBlobService blobService) : IRequestHandler<CleanupIntermediateAssetsCommand>
{
    /// <summary>Handles deletion of intermediate subtitle assets.</summary>
    public async Task Handle(CleanupIntermediateAssetsCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        foreach (var part in processedPost.Parts)
        {
            if (part.SubtitleBlobPath != null)
            {
                await blobService.DeleteAsync(
                    part.SubtitleBlobPath.ContainerName,
                    part.SubtitleBlobPath.AssetPath,
                    cancellationToken);
                part.SubtitleBlobPath = null;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
