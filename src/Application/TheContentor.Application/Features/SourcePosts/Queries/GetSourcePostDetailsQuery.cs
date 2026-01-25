using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.SourcePosts.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.SourcePosts.Queries;

public record GetSourcePostDetailsQuery(Guid Id) : IRequest<SourcePostDetailsDto?>;

public class GetSourcePostDetailsQueryHandler(TheContentorDbContext dbContext)
    : IRequestHandler<GetSourcePostDetailsQuery, SourcePostDetailsDto?>
{
    public async Task<SourcePostDetailsDto?> Handle(GetSourcePostDetailsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.SourcePosts
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
                Status = x.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
