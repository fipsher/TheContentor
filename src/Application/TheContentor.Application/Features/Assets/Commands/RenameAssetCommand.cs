using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Assets.Commands;

/// <summary>Renames an existing asset and updates its content tag.</summary>
public record RenameAssetCommand(Guid Id, string NewName, AssetContentTag? ContentTag) : IRequest<bool>;

/// <summary>Handles <see cref="RenameAssetCommand"/>.</summary>
public class RenameAssetCommandHandler(TheContentorDbContext context)
    : IRequestHandler<RenameAssetCommand, bool>
{
    /// <inheritdoc/>
    public async Task<bool> Handle(RenameAssetCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Assets
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (entity == null) return false;

        var nameConflict = await context.Assets
            .AnyAsync(a => a.Name == request.NewName && a.Id != request.Id, cancellationToken);

        if (nameConflict)
            throw new InvalidOperationException($"Asset name '{request.NewName}' is already taken.");

        entity.Name = request.NewName;
        entity.ContentTag = request.ContentTag;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
