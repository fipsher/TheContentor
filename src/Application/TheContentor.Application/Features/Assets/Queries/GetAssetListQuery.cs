using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Assets.Queries;

public record GetAssetListQuery : IRequest<List<AssetDto>>;

public class GetAssetListQueryHandler(TheContentorDbContext context) : IRequestHandler<GetAssetListQuery, List<AssetDto>>
{
    public async Task<List<AssetDto>> Handle(GetAssetListQuery request, CancellationToken cancellationToken)
    {
        return await context.Assets
            .AsNoTracking()
            .Select(b => new AssetDto
            {
                Id = b.Id,
                FileName = b.FileName,
                LocalPath = b.BlobPath,
                Tags = b.Tags,
                Duration = b.Duration,
                IsActive = b.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
