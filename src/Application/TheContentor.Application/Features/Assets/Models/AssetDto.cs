using TheContentor.Domain.Entities;

namespace TheContentor.Application.Features.Assets.Models;

/// <summary>Projection of a stored asset with access information.</summary>
public record AssetDto
{
    /// <summary>Asset identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Original file name.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Tag string used for filtering.</summary>
    public string Tags { get; set; } = string.Empty;
    /// <summary>Asset duration when known.</summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>Whether the asset is currently active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Blob path for storage location.</summary>
    public BlobPath BlobPath { get; set; } = null!;
    /// <summary>Time-limited SAS URI for access.</summary>
    public Uri SasUri { get; set; } = null!;
}
