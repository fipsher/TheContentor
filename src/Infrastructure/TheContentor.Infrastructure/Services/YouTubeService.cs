using TheContentor.Domain.Enums;
using TheContentor.Infrastructure.Interfaces;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace TheContentor.Infrastructure.Services;

public class YouTubeService(YoutubeClient youtube, YoutubeDL ytdl) : IYouTubeService
{
    private static readonly YouTubeVideoQuality[] AllQualities =
    [
        YouTubeVideoQuality.P144,
        YouTubeVideoQuality.P360,
        YouTubeVideoQuality.P480,
        YouTubeVideoQuality.P720,
        YouTubeVideoQuality.P1080,
        YouTubeVideoQuality.P1440,
        YouTubeVideoQuality.P2160
    ];

    public Task<bool> IsValidYouTubeUrlAsync(string url)
    {
        return Task.FromResult(VideoId.TryParse(url) != null);
    }

    public async Task<(TimeSpan Duration, string OriginalUrl, string Title)?> GetVideoMetadataAsync(string url)
    {
        try
        {
            var video = await youtube.Videos.GetAsync(url);
            return (video.Duration ?? TimeSpan.Zero, url, video.Title);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<YouTubeVideoQuality>> GetAvailableQualitiesAsync(string url)
    {
        try
        {
            var res = await ytdl.RunVideoDataFetch(url);
            if (!res.Success || res.Data?.Formats == null)
                return Array.Empty<YouTubeVideoQuality>();

            // Use the shorter dimension so quality labels are orientation-independent:
            // landscape 1080p  → width=1920, height=1080 → min=1080 ✓
            // portrait  1080p  → width=1080, height=1920 → min=1080 ✓
            var availableDimensions = res.Data.Formats
                .Where(f => f.Height.HasValue)
                .Select(f => f.Width.HasValue
                    ? Math.Min((int)f.Height!.Value, (int)f.Width!.Value)
                    : (int)f.Height!.Value)
                .ToHashSet();

            return AllQualities
                .Where(q => availableDimensions.Any(d => d >= (int)q - 30 && d <= (int)q + 30))
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<YouTubeVideoQuality>();
        }
    }

    public async Task<FileInfo?> DownloadVideoStreamAsync(YouTubeVideoQuality quality, string url)
    {
        try
        {
            var options = new OptionSet
            {
                Format = GetFormat(quality),
                MergeOutputFormat = DownloadMergeFormat.Mp4,
                RestrictFilenames = true,
                WriteInfoJson = false,
            };

            var res = await ytdl.RunVideoDownload(url, overrideOptions: options);
            return new FileInfo(res.Data);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string GetFormat(YouTubeVideoQuality quality)
    {
        var maxDim = (int)quality;
        // Landscape: height-constrained. Portrait: width-constrained fallback.
        // No [ext=] filter — MergeOutputFormat=Mp4 handles container conversion.
        return $"bestvideo[height<={maxDim}]/bestvideo[width<={maxDim}]";
    }
}
