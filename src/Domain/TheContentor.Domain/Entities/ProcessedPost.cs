using System.Diagnostics;
using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class ProcessedPost : BaseEntity
{
    public SourcePost SourcePost { get; set; } = null!;
    
    public List<string> Hashtags { get; set; } = new();
    public List<ProcessedPostPart> Parts { get; set; } = new();
}
