namespace TheContentor.Application.Features.ProcessedPosts.Models;

/// <summary>Video generation settings for a processed post.</summary>
public record VideoSettingsModel
{
    /// <summary>List of asset IDs to use as background video.</summary>
    public List<Guid> AssetIds { get; set; } = [];
}
