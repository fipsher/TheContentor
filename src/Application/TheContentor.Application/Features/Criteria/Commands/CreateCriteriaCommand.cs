using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Commands;

/// <summary>Creates a new analysis criterion.</summary>
public record CreateCriteriaCommand : IRequest<Guid>
{
    /// <summary>Display name for the criterion.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>System prompt used for analysis.</summary>
    public string SystemPrompt { get; set; } = string.Empty;
    /// <summary>Engine used to evaluate the criterion.</summary>
    public CriteriaEngine Engine { get; set; }
}

/// <summary>Handles creation of analysis criteria.</summary>
public class CreateCriteriaCommandHandler(TheContentorDbContext context) : IRequestHandler<CreateCriteriaCommand, Guid>
{
    /// <summary>Persists the new criterion and returns its id.</summary>
    public async Task<Guid> Handle(CreateCriteriaCommand request, CancellationToken cancellationToken)
    {
        var entity = new AnalysisCriteria
        {
            Name = request.Name,
            SystemPrompt = request.SystemPrompt,
            IsActive = true,
            Engine = request.Engine
        };

        context.AnalysisCriteria.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
