using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Queries;

public record GetAssetListQuery : IRequest<List<AssetDto>>;

public class GetAssetListQueryHandler(TheContentorDbContext context, IBlobService blobService) : IRequestHandler<GetAssetListQuery, List<AssetDto>>
{
    public async Task<List<AssetDto>> Handle(GetAssetListQuery request, CancellationToken cancellationToken)
    {
        var assets = await context.Assets
            .AsNoTracking()
            .Select(b => new AssetDto
            {
                Id = b.Id,
                FileName = b.Name,
                Tags = b.Tags,
                Duration = b.Duration,
                IsActive = b.IsActive
            })
            .ToListAsync(cancellationToken);

        var tasks = assets.Select(async asset =>
        {
            asset.SasUri = await blobService.GetSasUrl(asset.BlobPath.ContainerName, asset.BlobPath.AssetPath,
                cancellationToken);
        });

        await Task.WhenAll(tasks);

        return assets;
    }
}
