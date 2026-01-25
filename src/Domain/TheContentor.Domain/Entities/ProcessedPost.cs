using System.Diagnostics;
using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class ProcessedPost : BaseEntity
{
    public SourcePost SourcePost { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public List<string> Hashtags { get; set; } = new();
    public List<ProcessedPostPart> Parts { get; set; } = new();
}
