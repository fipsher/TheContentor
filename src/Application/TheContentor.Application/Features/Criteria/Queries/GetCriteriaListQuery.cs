using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Criteria.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Queries;

public record GetCriteriaListQuery : IRequest<List<CriteriaDto>>;

public class GetCriteriaListQueryHandler(TheContentorDbContext context) : IRequestHandler<GetCriteriaListQuery, List<CriteriaDto>>
{
    public async Task<List<CriteriaDto>> Handle(GetCriteriaListQuery request, CancellationToken cancellationToken)
    {
        return await context.AnalysisCriteria
            .AsNoTracking()
            .Select(c => new CriteriaDto
            {
                Id = c.Id,
                Name = c.Name,
                SystemPrompt = c.SystemPrompt,
                IsActive = c.IsActive,
                Engine = c.Engine
            })
            .ToListAsync(cancellationToken);
    }
}
