using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.SourcePosts.Models;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.SourcePosts.Queries;

/// <summary>Requests the list of source posts.</summary>
public record GetSourcePostListQuery : IRequest<List<SourcePostListItemDto>>;

/// <summary>Loads source post list projections.</summary>
public class GetSourcePostListQueryHandler(TheContentorDbContext dbContext)
    : IRequestHandler<GetSourcePostListQuery, List<SourcePostListItemDto>>
{
    /// <summary>Returns source posts ordered by ingestion time.</summary>
    public async Task<List<SourcePostListItemDto>> Handle(GetSourcePostListQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.SourcePosts
            .AsNoTracking()
            .Select(x => new SourcePostListItemDto
            {
                Id = x.Id,
                Platform = x.Platform,
                Community = x.Community,
                Title = x.Title,
                WordCount = x.WordCount,
                Score = x.Score,
                UpvoteRatio = x.UpvoteRatio,
                CreatedUtc = x.CreatedUtc,
                IngestedUtc = x.IngestedUtc,
                Status = x.Status,
                ExternalUrl = x.ExternalUrl,
                TtsStatus = x.ProcessedPost != null ? (TtsStatus?)x.ProcessedPost.TtsStatus : null,
                VideoStatus = x.ProcessedPost != null ? (VideoStatus?)x.ProcessedPost.VideoStatus : null,
                IsPosted = x.ProcessedPost != null ? (bool?)x.ProcessedPost.IsPosted : null,
                ProcessedPostId = x.ProcessedPost != null ? (Guid?)x.ProcessedPost.Id : null
            })
            .OrderByDescending(x => x.IngestedUtc)
            .ToListAsync(cancellationToken);
    }
}
