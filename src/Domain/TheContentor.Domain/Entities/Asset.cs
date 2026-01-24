using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class Asset : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public required BlobPath BlobPath { get; set; }
    public string Tags { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
    public bool IsActive { get; set; }
}