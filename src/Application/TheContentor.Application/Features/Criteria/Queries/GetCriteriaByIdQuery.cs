using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Criteria.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Queries;

public record GetCriteriaByIdQuery(Guid Id) : IRequest<CriteriaDto?>;

public class GetCriteriaByIdQueryHandler(TheContentorDbContext context) : IRequestHandler<GetCriteriaByIdQuery, CriteriaDto?>
{
    public async Task<CriteriaDto?> Handle(GetCriteriaByIdQuery request, CancellationToken cancellationToken)
    {
        return await context.AnalysisCriteria
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CriteriaDto
            {
                Id = c.Id,
                Name = c.Name,
                SystemPrompt = c.SystemPrompt,
                IsActive = c.IsActive,
                Engine = c.Engine
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
