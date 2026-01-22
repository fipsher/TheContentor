using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class ContentAnalysis : BaseEntity
{
    public SourcePost SourcePost { get; set; } = null!;
    public float AttractivenessScore { get; set; }
    public string[] Labels { get; set; } = [];
    public string AiReasoning { get; set; } = string.Empty;
    public AnalysisCriteria Criteria { get; set; } = null!;
}
