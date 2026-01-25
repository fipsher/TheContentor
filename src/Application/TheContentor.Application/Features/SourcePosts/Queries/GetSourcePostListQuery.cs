using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.SourcePosts.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.SourcePosts.Queries;

public record GetSourcePostListQuery : IRequest<List<SourcePostListItemDto>>;

public class GetSourcePostListQueryHandler(TheContentorDbContext dbContext)
    : IRequestHandler<GetSourcePostListQuery, List<SourcePostListItemDto>>
{
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
                ExternalUrl = x.ExternalUrl
            })
            .OrderByDescending(x => x.IngestedUtc)
            .ToListAsync(cancellationToken);
    }
}
