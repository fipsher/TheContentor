using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Application.Features.SourcePosts.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.ProcessedPosts.Queries;

public record GetProcessedPostByIdQuery(Guid Id) : IRequest<ProcessedPostDetailsDto?>;

public class GetProcessedPostByIdQueryHandler(TheContentorDbContext dbContext)
    : IRequestHandler<GetProcessedPostByIdQuery, ProcessedPostDetailsDto?>
{
    public async Task<ProcessedPostDetailsDto?> Handle(GetProcessedPostByIdQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.ProcessedPosts
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new ProcessedPostDetailsDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Hashtags = x.Hashtags,
                NarratorGender = x.NarratorGender,
                TtsStatus = x.TtsStatus,
                TtsSettings = x.TtsSettings,
                DescriptionAudioBlobPath = x.DescriptionAudioBlobPath == null ? null : new BlobPathDto
                {
                    ContainerName = x.DescriptionAudioBlobPath.ContainerName,
                    AssetPath = x.DescriptionAudioBlobPath.AssetPath
                },
                Parts = x.Parts.Select(p => new ProcessedPostPartDto
                {
                    Id = p.Id,
                    ProcessedText = p.ProcessedText,
                    Hashtags = p.Hashtags,
                    PublishedTo = p.PublishedTo,
                    Part = p.Part,
                    AudioBlobPath = p.AudioBlobPath == null ? null : new BlobPathDto
                    {
                        ContainerName = p.AudioBlobPath.ContainerName,
                        AssetPath = p.AudioBlobPath.AssetPath
                    }
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
