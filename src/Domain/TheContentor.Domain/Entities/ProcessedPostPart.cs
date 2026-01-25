using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class ProcessedPostPart : BaseEntity
{
    public Guid ProcessedPostId { get; set; }
    public ProcessedPost ProcessedPost { get; set; } = null!;
    
    public int Part { get; set; }
    public string ProcessedText { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
    public List<SocialPlatform> PublishedTo { get; set; } = new();
}
