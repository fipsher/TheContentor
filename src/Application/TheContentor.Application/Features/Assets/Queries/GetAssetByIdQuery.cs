using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Queries;

public record GetAssetByIdQuery(Guid Id) : IRequest<AssetDto?>;

public class GetAssetByIdQueryHandler(TheContentorDbContext context, IBlobService blobService) : IRequestHandler<GetAssetByIdQuery, AssetDto?>
{
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
                Tags = b.Tags,
                Duration = b.Duration,
                IsActive = b.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (asset != null)
        {
            asset.SasUri = await blobService.GetSasUrl(asset.BlobPath.ContainerName, asset.BlobPath.AssetPath, cancellationToken);
        }
        
        return asset;
    }
}
