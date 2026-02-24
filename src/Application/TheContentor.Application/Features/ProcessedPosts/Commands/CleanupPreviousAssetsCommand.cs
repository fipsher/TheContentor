using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Deletes all previous generation assets before a re-run.</summary>
public record CleanupPreviousAssetsCommand(Guid ProcessedPostId) : IRequest;

/// <summary>Removes TTS audio, video, and subtitle files from storage and resets entity state.</summary>
public class CleanupPreviousAssetsCommandHandler(
    TheContentorDbContext context,
    IBlobService blobService) : IRequestHandler<CleanupPreviousAssetsCommand>
{
    /// <summary>Handles deletion of all previous generation assets.</summary>
    public async Task Handle(CleanupPreviousAssetsCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        if (processedPost.TtsStatus == TtsStatus.InProgress ||
            processedPost.VideoStatus == VideoStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Cannot clean up assets while generation is in progress. Cancel generation first.");
        }

        foreach (var part in processedPost.Parts)
        {
            if (part.AudioBlobPath != null)
            {
                await blobService.DeleteAsync(part.AudioBlobPath.ContainerName, part.AudioBlobPath.AssetPath, cancellationToken);
                part.AudioBlobPath = null;
            }

            if (part.VideoBlobPath != null)
            {
                await blobService.DeleteAsync(part.VideoBlobPath.ContainerName, part.VideoBlobPath.AssetPath, cancellationToken);
                part.VideoBlobPath = null;
            }

            if (part.SubtitleBlobPath != null)
            {
                await blobService.DeleteAsync(part.SubtitleBlobPath.ContainerName, part.SubtitleBlobPath.AssetPath, cancellationToken);
                part.SubtitleBlobPath = null;
            }

            part.AudioDuration = null;
        }

        if (processedPost.DescriptionAudioBlobPath != null)
        {
            await blobService.DeleteAsync(
                processedPost.DescriptionAudioBlobPath.ContainerName,
                processedPost.DescriptionAudioBlobPath.AssetPath,
                cancellationToken);
            processedPost.DescriptionAudioBlobPath = null;
        }

        if (processedPost.VideoBlobPath != null)
        {
            await blobService.DeleteAsync(
                processedPost.VideoBlobPath.ContainerName,
                processedPost.VideoBlobPath.AssetPath,
                cancellationToken);
            processedPost.VideoBlobPath = null;
        }

        processedPost.TtsStatus = TtsStatus.NotGenerated;
        processedPost.VideoStatus = VideoStatus.NotGenerated;

        await context.SaveChangesAsync(cancellationToken);
    }
}
