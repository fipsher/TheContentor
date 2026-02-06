using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class Asset : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public required BlobPath BlobPath { get; set; }
    public string Tags { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
    public bool IsActive { get; set; }

    // New properties for YouTube assets
    public string? OriginalUrl { get; set; } // YouTube URL
    public string? Title { get; set; } // YouTube video title
    public int? Width { get; set; } // Video resolution width
    public int? Height { get; set; } // Video resolution height
    public DateTime? UploadDate { get; set; } // YouTube upload date

    // New property for asset type
    public AssetType Type { get; set; } = AssetType.ManualUpload; // Default to ManualUpload
}