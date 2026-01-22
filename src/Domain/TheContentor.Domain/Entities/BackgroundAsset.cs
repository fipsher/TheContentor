using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class BackgroundAsset : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsActive { get; set; }
}
