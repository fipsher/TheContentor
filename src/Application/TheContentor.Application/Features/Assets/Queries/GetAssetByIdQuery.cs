using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Queries;

/// <summary>Requests a single asset by identifier.</summary>
public record GetAssetByIdQuery(Guid Id) : IRequest<AssetDto?>;

/// <summary>Loads asset metadata and SAS URL by id.</summary>
public class GetAssetByIdQueryHandler(TheContentorDbContext context, IBlobService blobService) : IRequestHandler<GetAssetByIdQuery, AssetDto?>
{
    /// <summary>Returns the asset projection when found.</summary>
    public async Task<AssetDto?> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var asset = await context.Assets
            .AsNoTracking()
            .Where(b => b.Id == request.Id)
            .Select(b => new AssetDto
            {
                Id = b.Id,
                FileName = b.Name,
                BlobPath = b.BlobPath,
                ContentTag = b.ContentTag,
                Duration = b.Duration,
                IsActive = b.IsActive,
                Type = b.Type,
                OriginalUrl = b.OriginalUrl,
                Quality = b.Quality,
                Title = b.Title,
                ThumbnailPath = b.ThumbnailPath
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (asset != null)
        {
            asset.SasUri = await blobService.GetSasUrl(asset.BlobPath.ContainerName, asset.BlobPath.AssetPath, cancellationToken);
        }
        
        return asset;
    }
}
