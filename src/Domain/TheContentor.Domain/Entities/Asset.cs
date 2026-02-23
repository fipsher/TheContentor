using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class Asset : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public required BlobPath BlobPath { get; set; }
    /// <summary>Predefined content tag. Null means untagged.</summary>
    public AssetContentTag? ContentTag { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool IsActive { get; set; }

    // New properties for YouTube assets
    public string? OriginalUrl { get; set; } // YouTube URL
    public string? Title { get; set; } // YouTube video title

    // New property for asset type
    public AssetType Type { get; set; } = AssetType.ManualUpload; // Default to ManualUpload

    /// <summary>Quality of the downloaded YouTube video. Null for manually uploaded assets.</summary>
    public YouTubeVideoQuality? Quality { get; set; }

    /// <summary>Relative file name of the generated thumbnail PNG in the "asset-thumbnails" storage container. Null until generated.</summary>
    public string? ThumbnailPath { get; set; }
}