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

            var availableHeights = res.Data.Formats
                .Where(f => f.Height.HasValue)
                .Select(f => (int)f.Height!.Value)
                .ToHashSet();

            return AllQualities
                .Where(q => availableHeights.Any(h => h >= (int)q - 30 && h <= (int)q + 30))
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

    private static string GetFormat(YouTubeVideoQuality quality, string extension = "mp4")
    {
        var maxHeight = (int)quality;
        return $"bestvideo[height<={maxHeight}][ext={extension}]";
    }
}
