using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class SourcePost : BaseEntity
{
    public SourcePlatform Platform { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public SourcePostStatus Status { get; set; }
    public List<SourceComment> Comments { get; set; } = new();
}
