using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class Asset : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsActive { get; set; }
}
