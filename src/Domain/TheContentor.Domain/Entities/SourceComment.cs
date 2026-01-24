using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class SourceComment : BaseEntity
{
    public Guid SourcePostId { get; set; }
    public SourcePost SourcePost { get; set; } = null!;

    public string ExternalId { get; set; } = string.Empty;
    public string ParentExternalId { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public int Score { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }
    public bool IsDeleted { get; set; }
    public string MetadataJson { get; set; } = "{}";
}