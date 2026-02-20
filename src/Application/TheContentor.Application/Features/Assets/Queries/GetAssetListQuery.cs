using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Queries;

/// <summary>Requests the list of asset metadata.</summary>
public record GetAssetListQuery : IRequest<List<AssetDto>>;

/// <summary>Loads assets and enriches them with SAS URLs.</summary>
public class GetAssetListQueryHandler(TheContentorDbContext context, IBlobService blobService) : IRequestHandler<GetAssetListQuery, List<AssetDto>>
{
    /// <summary>Returns asset projections with SAS URIs attached.</summary>
    public async Task<List<AssetDto>> Handle(GetAssetListQuery request, CancellationToken cancellationToken)
    {
        var assets = await context.Assets
            .Include(b => b.BlobPath)
            .AsNoTracking()
            .Select(b => new AssetDto
            {
                Id = b.Id,
                FileName = b.Name,
                Tags = b.Tags,
                Duration = b.Duration,
                IsActive = b.IsActive,
                BlobPath = b.BlobPath,
                Type = b.Type,
                OriginalUrl = b.OriginalUrl,
                Quality = b.Quality
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
