using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.SourcePosts.Models;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.SourcePosts.Queries;

/// <summary>Requests detailed source post data by id.</summary>
public record GetSourcePostDetailsQuery(Guid Id) : IRequest<SourcePostDetailsDto?>;

/// <summary>Loads source post details and enriches blob paths.</summary>
public class GetSourcePostDetailsQueryHandler(TheContentorDbContext dbContext, IBlobService blobService)
    : IRequestHandler<GetSourcePostDetailsQuery, SourcePostDetailsDto?>
{
    /// <summary>Returns source post details with SAS URLs when available.</summary>
    public async Task<SourcePostDetailsDto?> Handle(GetSourcePostDetailsQuery request, CancellationToken cancellationToken)
    {
        var post = await dbContext.SourcePosts
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new SourcePostDetailsDto
            {
                Id = x.Id,
                Platform = x.Platform,
                Community = x.Community,
                Title = x.Title,
                AuthorName = x.AuthorName,
                RawText = x.RawText,
                WordCount = x.WordCount,
                Score = x.Score,
                UpvoteRatio = x.UpvoteRatio,
                CreatedUtc = x.CreatedUtc,
                ExternalUrl = x.ExternalUrl,
                Status = x.Status,
                ProcessedPost = x.ProcessedPost == null ? null : new ProcessedPostDto
                {
                    Id = x.ProcessedPost.Id,
                    Title = x.ProcessedPost.Title,
                    Description = x.ProcessedPost.Description,
                    Hashtags = x.ProcessedPost.Hashtags,
                    NarratorGender = x.ProcessedPost.NarratorGender,
                    TtsStatus = x.ProcessedPost.TtsStatus,
                    TtsSettings = x.ProcessedPost.TtsSettings,
                    DescriptionAudioBlobPath = x.ProcessedPost.DescriptionAudioBlobPath == null ? null : new BlobPathDto
                    {
                        ContainerName = x.ProcessedPost.DescriptionAudioBlobPath.ContainerName,
                        AssetPath = x.ProcessedPost.DescriptionAudioBlobPath.AssetPath
                    },
                    Parts = x.ProcessedPost.Parts.Select(p => new ProcessedPostPartDto
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
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (post?.ProcessedPost != null)
        {
            if (post.ProcessedPost.DescriptionAudioBlobPath != null)
            {
                var sasUrl = await blobService.GetSasUrl(
                    post.ProcessedPost.DescriptionAudioBlobPath.ContainerName,
                    post.ProcessedPost.DescriptionAudioBlobPath.AssetPath,
                    cancellationToken);
                post.ProcessedPost.DescriptionAudioBlobPath.SasUrl = sasUrl.ToString();
            }

            foreach (var part in post.ProcessedPost.Parts)
            {
                if (part.AudioBlobPath != null)
                {
                    var sasUrl = await blobService.GetSasUrl(
                        part.AudioBlobPath.ContainerName,
                        part.AudioBlobPath.AssetPath,
                        cancellationToken);
                    part.AudioBlobPath.SasUrl = sasUrl.ToString();
                }
            }
        }

        return post;
    }
}
