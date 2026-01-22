using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class SourceComment : BaseEntity
{
    public SourcePost SourcePost { get; set; } = null!;
    public string Author { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsIncluded { get; set; }
}
