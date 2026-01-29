using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Assets.Commands;

/// <summary>Toggles active state for an asset.</summary>
public record ToggleAssetStatusCommand(Guid Id) : IRequest<bool>;

/// <summary>Handles activation toggles for assets.</summary>
public class ToggleAssetStatusCommandHandler(TheContentorDbContext context) : IRequestHandler<ToggleAssetStatusCommand, bool>
{
    /// <summary>Flips the active flag and returns whether the entity exists.</summary>
    public async Task<bool> Handle(ToggleAssetStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Assets
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        entity.IsActive = !entity.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
