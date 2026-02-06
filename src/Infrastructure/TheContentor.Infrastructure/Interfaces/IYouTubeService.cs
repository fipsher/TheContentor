namespace TheContentor.Infrastructure.Interfaces;

public interface IYouTubeService
{
    /// <summary>
    /// Validates if the provided URL is a valid YouTube video URL.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the URL is a valid YouTube video URL, false otherwise.</returns>
    Task<bool> IsValidYouTubeUrlAsync(string url);

    /// <summary>
    /// Extracts metadata from a YouTube video URL.
    /// </summary>
    /// <param name="url">The YouTube video URL.</param>
    /// <returns>A tuple containing video duration, resolution, upload date, original URL, and title, or null if extraction fails.</returns>
    Task<(TimeSpan Duration, string OriginalUrl, string Title)?> GetVideoMetadataAsync(string url);

    /// <summary>
    /// Downloads the video stream from a YouTube video URL.
    /// </summary>
    /// <param name="url">The YouTube video URL.</param>
    /// <returns>A stream of the video content, or null if download fails.</returns>
    Task<FileInfo?> DownloadVideoStreamAsync(string url);
}
