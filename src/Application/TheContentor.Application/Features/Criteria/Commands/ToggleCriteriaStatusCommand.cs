using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Commands;

public record ToggleCriteriaStatusCommand(Guid Id) : IRequest<bool>;

public class ToggleCriteriaStatusCommandHandler(TheContentorDbContext context) : IRequestHandler<ToggleCriteriaStatusCommand, bool>
{
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
