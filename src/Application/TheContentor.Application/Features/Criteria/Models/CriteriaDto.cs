using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.Criteria.Models;

/// <summary>Projection of an analysis criterion.</summary>
public class CriteriaDto
{
    /// <summary>Criterion identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Display name for the criterion.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>System prompt used for analysis.</summary>
    public string SystemPrompt { get; set; } = string.Empty;
    /// <summary>Whether the criterion is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Engine used to evaluate the criterion.</summary>
    public CriteriaEngine Engine { get; set; }
}
