using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.Assets.Models;

/// <summary>Projection of a stored asset with access information.</summary>
public record AssetDto
{
    /// <summary>Asset identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Original file name.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Predefined content tag. Null means untagged.</summary>
    public AssetContentTag? ContentTag { get; set; }
    /// <summary>Asset duration when known.</summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>Whether the asset is currently active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Blob path for storage location.</summary>
    public BlobPath BlobPath { get; set; } = null!;
    /// <summary>Time-limited SAS URI for access.</summary>
    public Uri SasUri { get; set; } = null!;
    /// <summary>Whether this is a manually uploaded or YouTube asset.</summary>
    public AssetType Type { get; set; }
    /// <summary>Original YouTube URL. Null for manually uploaded assets.</summary>
    public string? OriginalUrl { get; set; }
    /// <summary>Quality of the YouTube video. Null for manually uploaded assets.</summary>
    public YouTubeVideoQuality? Quality { get; set; }
    /// <summary>Video title. Populated for YouTube assets.</summary>
    public string? Title { get; set; }
    /// <summary>Relative thumbnail file name from the entity.</summary>
    public string? ThumbnailPath { get; set; }
    /// <summary>Thumbnail URL served via /storage/ static middleware. Null until generated.</summary>
    public string? ThumbnailUrl { get; set; }
}
