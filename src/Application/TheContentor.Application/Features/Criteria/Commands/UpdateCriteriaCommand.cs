using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Criteria.Commands;

public record UpdateCriteriaCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public CriteriaEngine Engine { get; set; }
}

public class UpdateCriteriaCommandHandler(TheContentorDbContext context) : IRequestHandler<UpdateCriteriaCommand, bool>
{
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
