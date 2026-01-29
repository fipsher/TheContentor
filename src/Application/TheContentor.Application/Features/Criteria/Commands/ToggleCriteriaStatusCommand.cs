using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Commands;

/// <summary>Toggles active state for an analysis criterion.</summary>
public record ToggleCriteriaStatusCommand(Guid Id) : IRequest<bool>;

/// <summary>Handles activation toggles for criteria.</summary>
public class ToggleCriteriaStatusCommandHandler(TheContentorDbContext context) : IRequestHandler<ToggleCriteriaStatusCommand, bool>
{
    /// <summary>Flips the active flag and returns whether the entity exists.</summary>
    public async Task<bool> Handle(ToggleCriteriaStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.AnalysisCriteria
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        entity.IsActive = !entity.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
