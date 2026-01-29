using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Commands;

/// <summary>Updates an existing analysis criterion.</summary>
public record UpdateCriteriaCommand : IRequest<bool>
{
    /// <summary>Criterion identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Updated display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Updated system prompt content.</summary>
    public string SystemPrompt { get; set; } = string.Empty;
    /// <summary>Updated evaluation engine.</summary>
    public CriteriaEngine Engine { get; set; }
}

/// <summary>Handles updates to analysis criteria.</summary>
public class UpdateCriteriaCommandHandler(TheContentorDbContext context) : IRequestHandler<UpdateCriteriaCommand, bool>
{
    /// <summary>Applies updates and returns whether the entity exists.</summary>
    public async Task<bool> Handle(UpdateCriteriaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.AnalysisCriteria
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        entity.Name = request.Name;
        entity.SystemPrompt = request.SystemPrompt;
        entity.Engine = request.Engine;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
