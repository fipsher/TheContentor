using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>Updates TTS status and audio blob paths after generation.</summary>
public record UpdateTtsStatusCommand(
    Guid ProcessedPostId,
    TtsStatus Status,
    BlobPath? DescriptionAudioBlobPath,
    Dictionary<Guid, BlobPath> PartAudioBlobPaths,
    Dictionary<Guid, double?> PartAudioDurations) : IRequest;

/// <summary>Persists TTS status changes for a processed post.</summary>
public class UpdateTtsStatusCommandHandler(
    TheContentorDbContext context) : IRequestHandler<UpdateTtsStatusCommand>
{
    /// <summary>Applies TTS status updates and audio paths.</summary>
    public async Task Handle(UpdateTtsStatusCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            throw new InvalidOperationException($"ProcessedPost with ID {request.ProcessedPostId} not found");
        }

        processedPost.TtsStatus = request.Status;
        processedPost.DescriptionAudioBlobPath = request.DescriptionAudioBlobPath;

        foreach (var (partId, blobPath) in request.PartAudioBlobPaths)
        {
            var part = processedPost.Parts.FirstOrDefault(p => p.Id == partId);
            if (part != null)
            {
                part.AudioBlobPath = blobPath;

                if (request.PartAudioDurations.TryGetValue(partId, out var durationSeconds) && durationSeconds.HasValue)
                {
                    part.AudioDuration = TimeSpan.FromSeconds(durationSeconds.Value);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
