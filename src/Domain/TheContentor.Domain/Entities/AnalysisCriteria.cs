using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class AnalysisCriteria : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    public CriteriaEngine Engine { get; set; }
}
