using TheContentor.Domain.Enums;

namespace TheContentor.Infrastructure.Interfaces;

public interface IYouTubeService
{
    /// <summary>Validates if the provided URL is a valid YouTube video URL.</summary>
    Task<bool> IsValidYouTubeUrlAsync(string url);

    /// <summary>Extracts metadata from a YouTube video URL.</summary>
    Task<(TimeSpan Duration, string OriginalUrl, string Title)?> GetVideoMetadataAsync(string url);

    /// <summary>Returns the quality tiers available for the given YouTube video.</summary>
    Task<IReadOnlyList<YouTubeVideoQuality>> GetAvailableQualitiesAsync(string url);

    /// <summary>Downloads the video at the requested quality (falls back to closest lower tier if unavailable).</summary>
    Task<FileInfo?> DownloadVideoStreamAsync(YouTubeVideoQuality quality, string url);
}
