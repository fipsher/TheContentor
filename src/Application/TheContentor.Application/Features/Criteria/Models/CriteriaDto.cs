using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.Criteria.Models;

public class CriteriaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public CriteriaEngine Engine { get; set; }
}
