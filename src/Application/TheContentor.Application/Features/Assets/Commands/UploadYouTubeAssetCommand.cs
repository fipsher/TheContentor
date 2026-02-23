using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Commands;

/// <summary>Downloads a YouTube video and stores it as an asset.</summary>
public record UploadYouTubeAssetCommand(
    string YouTubeUrl,
    string Name,
    YouTubeVideoQuality Quality,
    AssetContentTag? ContentTag = null) : IRequest<Guid>;

/// <summary>Handles YouTube video asset uploads and persistence.</summary>
public class UploadYouTubeAssetCommandHandler(
    TheContentorDbContext context,
    IYouTubeService youtubeService,
    IBlobService blobService) : IRequestHandler<UploadYouTubeAssetCommand, Guid>
{
    /// <summary>Validates the URL, downloads the video, stores it, and persists the asset metadata.</summary>
    public async Task<Guid> Handle(UploadYouTubeAssetCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate YouTube URL
        if (!await youtubeService.IsValidYouTubeUrlAsync(request.YouTubeUrl))
            throw new ArgumentException("Invalid YouTube URL provided.", nameof(request.YouTubeUrl));

        // 2. Get Video Metadata
        var metadata = await youtubeService.GetVideoMetadataAsync(request.YouTubeUrl);
        if (metadata == null)
            throw new InvalidOperationException("Could not retrieve video metadata from the provided URL.");

        // 3. Download Video
        var fileInfo = await youtubeService.DownloadVideoStreamAsync(request.Quality, request.YouTubeUrl);
        if (fileInfo == null)
            throw new InvalidOperationException("Could not download video from the provided URL.");

        try
        {
            await using var videoStream = fileInfo.OpenRead();

            // 4. Store in Blob Storage
            var blobFileName = $"youtube_videos/{Guid.NewGuid()}.mp4";
            var localPath = await blobService.UploadAsync(videoStream, "assets", blobFileName, "video/mp4", cancellationToken);

            if (localPath == null)
                throw new InvalidOperationException("Failed to upload video to blob storage.");

            // 5. Persist Asset
            var newAsset = new Asset
            {
                Name = request.Name,
                OriginalUrl = metadata.Value.OriginalUrl,
                Title = metadata.Value.Title,
                Duration = metadata.Value.Duration,
                Type = AssetType.YouTube,
                Quality = request.Quality,
                BlobPath = localPath,
                IsActive = true,
                ContentTag = request.ContentTag
            };

            context.Assets.Add(newAsset);
            await context.SaveChangesAsync(cancellationToken);

            return newAsset.Id;
        }
        finally
        {
            fileInfo.Delete();
        }
    }
}
