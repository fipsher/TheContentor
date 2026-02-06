using TheContentor.Infrastructure.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace TheContentor.Infrastructure.Services;

public class YouTubeService : IYouTubeService
{
    private readonly YoutubeClient _youtube = new();

    public Task<bool> IsValidYouTubeUrlAsync(string url)
    {
        return Task.FromResult(VideoId.TryParse(url) != null);
    }

    public async Task<(TimeSpan Duration, int Width, int Height, DateTime UploadDate, string OriginalUrl, string Title)?> GetVideoMetadataAsync(string url)
    {
        try
        {
            var video = await _youtube.Videos.GetAsync(url);
            
            // Get highest quality video stream info
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);
            var videoStreamInfo = streamManifest.GetVideoOnlyStreams().OrderByDescending(s => s.VideoResolution.Area).FirstOrDefault();
            
            if (videoStreamInfo == null)
            {
                return null;
            }

            return (video.Duration ?? TimeSpan.Zero, 
                    videoStreamInfo.VideoResolution.Width, 
                    videoStreamInfo.VideoResolution.Height, 
                    video.UploadDate.DateTime, 
                    video.Url, 
                    video.Title);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Stream?> DownloadVideoStreamAsync(string url)
    {
        try
        {
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(url);
            
            // Get highest quality video-only stream
            var streamInfo = streamManifest.GetVideoOnlyStreams().OrderByDescending(s => s.VideoResolution.Area).FirstOrDefault();

            if (streamInfo == null)
            {
                return null;
            }

            var memoryStream = new MemoryStream();
            await _youtube.Videos.Streams.CopyToAsync(streamInfo, memoryStream);
            memoryStream.Position = 0; // Reset position for reading
            return memoryStream;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
