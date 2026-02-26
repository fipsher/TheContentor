using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Deletes intermediate pipeline assets after successful generation.</summary>
public record CleanupIntermediateAssetsCommand(
    Guid ProcessedPostId,
    List<BlobPath>? AdditionalBlobPaths = null) : IRequest;

/// <summary>Removes TTS audio, subtitle files, and any additional blobs no longer needed after video composition.</summary>
public class CleanupIntermediateAssetsCommandHandler(
    TheContentorDbContext context,
    IBlobService blobService,
    ILogger<CleanupIntermediateAssetsCommandHandler> logger) : IRequestHandler<CleanupIntermediateAssetsCommand>
{
    /// <summary>Handles deletion of intermediate assets including TTS audio, subtitles, and additional blobs.</summary>
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
            if (part.AudioBlobPath != null)
            {
                await blobService.DeleteAsync(
                    part.AudioBlobPath.ContainerName,
                    part.AudioBlobPath.AssetPath,
                    cancellationToken);
                part.AudioBlobPath = null;
            }

            if (part.SubtitleBlobPath != null)
            {
                await blobService.DeleteAsync(
                    part.SubtitleBlobPath.ContainerName,
                    part.SubtitleBlobPath.AssetPath,
                    cancellationToken);
                part.SubtitleBlobPath = null;
            }
        }

        if (processedPost.DescriptionAudioBlobPath != null)
        {
            await blobService.DeleteAsync(
                processedPost.DescriptionAudioBlobPath.ContainerName,
                processedPost.DescriptionAudioBlobPath.AssetPath,
                cancellationToken);
            processedPost.DescriptionAudioBlobPath = null;
        }

        await context.SaveChangesAsync(cancellationToken);

        if (request.AdditionalBlobPaths != null)
        {
            foreach (var blob in request.AdditionalBlobPaths)
            {
                try
                {
                    await blobService.DeleteAsync(blob.ContainerName, blob.AssetPath, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete additional blob {Container}/{Path}", blob.ContainerName, blob.AssetPath);
                }
            }
        }
    }
}
