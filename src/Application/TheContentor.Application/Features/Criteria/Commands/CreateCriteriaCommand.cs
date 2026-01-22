using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Commands;

public record CreateCriteriaCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public CriteriaEngine Engine { get; set; }
}

public class CreateCriteriaCommandHandler(TheContentorDbContext context) : IRequestHandler<CreateCriteriaCommand, Guid>
{
    public async Task<Guid> Handle(CreateCriteriaCommand request, CancellationToken cancellationToken)
    {
        var entity = new AnalysisCriteria
        {
            Name = request.Name,
            SystemPrompt = request.SystemPrompt,
            IsActive = request.IsActive,
            Engine = request.Engine
        };

        context.AnalysisCriteria.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
