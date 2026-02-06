using TheContentor.Infrastructure.Interfaces;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace TheContentor.Infrastructure.Services;

public class YouTubeService(YoutubeClient youtube, YoutubeDL ytdl) : IYouTubeService
{
    public Task<bool> IsValidYouTubeUrlAsync(string url)
    {
        return Task.FromResult(VideoId.TryParse(url) != null);
    }

    public async Task<(TimeSpan Duration, string OriginalUrl, string Title)?> GetVideoMetadataAsync(string url)
    {
        try
        {
            var video = await youtube.Videos.GetAsync(url);
            
            return (video.Duration ?? TimeSpan.Zero, 
                    url, 
                    video.Title);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<FileInfo?> DownloadVideoStreamAsync(string url)
    {
        try
        {
            var options = new OptionSet
            {
                Format = "worstvideo[ext=mp4]",
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
}
