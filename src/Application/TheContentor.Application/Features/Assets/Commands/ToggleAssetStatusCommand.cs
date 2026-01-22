using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Assets.Commands;

public record ToggleAssetStatusCommand(Guid Id) : IRequest<bool>;

public class ToggleAssetStatusCommandHandler(TheContentorDbContext context) : IRequestHandler<ToggleAssetStatusCommand, bool>
{
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
