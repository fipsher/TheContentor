using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Assets.Queries;

public record GetAssetByIdQuery(Guid Id) : IRequest<AssetDto?>;

public class GetAssetByIdQueryHandler(TheContentorDbContext context) : IRequestHandler<GetAssetByIdQuery, AssetDto?>
{
    public async Task<AssetDto?> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        return await context.Assets
            .AsNoTracking()
            .Where(b => b.Id == request.Id)
            .Select(b => new AssetDto
            {
                Id = b.Id,
                FileName = b.FileName,
                LocalPath = b.BlobPath,
                Tags = b.Tags,
                Duration = b.Duration,
                IsActive = b.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
