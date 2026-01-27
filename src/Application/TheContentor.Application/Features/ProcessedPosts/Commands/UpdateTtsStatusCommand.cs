using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Commands;

/// <summary>
/// Command to update TTS status and blob paths after generation
/// </summary>
public record UpdateTtsStatusCommand(
    Guid ProcessedPostId,
    TtsStatus Status,
    BlobPath? DescriptionAudioBlobPath,
    Dictionary<Guid, BlobPath> PartAudioBlobPaths) : IRequest;

public class UpdateTtsStatusCommandHandler(
    TheContentorDbContext context) : IRequestHandler<UpdateTtsStatusCommand>
{
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
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
