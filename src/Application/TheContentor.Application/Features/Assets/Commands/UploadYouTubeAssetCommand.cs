using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Commands;

/// <summary>Uploads a YouTube video asset and its metadata.</summary>
public record UploadYouTubeAssetCommand(string YouTubeUrl) : IRequest<Guid>;

/// <summary>Handles YouTube video asset uploads and persistence.</summary>
public class UploadYouTubeAssetCommandHandler(
    TheContentorDbContext context,
    IYouTubeService youtubeService,
    IBlobService blobService) : IRequestHandler<UploadYouTubeAssetCommand, Guid>
{
    /// <summary>Uploads the YouTube asset, extracts metadata, stores video, and persists metadata.</summary>
    public async Task<Guid> Handle(UploadYouTubeAssetCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate YouTube URL
        if (!await youtubeService.IsValidYouTubeUrlAsync(request.YouTubeUrl))
        {
            throw new ArgumentException("Invalid YouTube URL provided.", nameof(request.YouTubeUrl));
        }

        // 2. Get Video Metadata
        var metadata = await youtubeService.GetVideoMetadataAsync(request.YouTubeUrl);
        if (metadata == null)
        {
            throw new InvalidOperationException("Could not retrieve video metadata from the provided URL.");
        }

        // 3. Download Video Stream
        var videoStream = await youtubeService.DownloadVideoStreamAsync(request.YouTubeUrl);
        if (videoStream == null)
        {
            throw new InvalidOperationException("Could not download video stream from the provided URL.");
        }

        // 4. Store Video in Blob Storage
        // Use a descriptive name for the blob, possibly including the title or ID
        var blobFileName = $"youtube_videos/{Guid.NewGuid()}.mp4"; 
        var localPath = await blobService.UploadAsync(videoStream, "assets", blobFileName, "video/mp4", cancellationToken);
        
        if (localPath == null)
        {
            throw new InvalidOperationException("Failed to upload video to blob storage.");
        }
        
        // 5. Create and Persist Asset Entity
        var newAsset = new Asset
        {
            Name = metadata.Value.Title, // Use YouTube title as asset name
            OriginalUrl = metadata.Value.OriginalUrl,
            Title = metadata.Value.Title,
            Duration = metadata.Value.Duration,
            Width = metadata.Value.Width,
            Height = metadata.Value.Height,
            UploadDate = metadata.Value.UploadDate,
            Type = AssetType.YouTube, // Mark as YouTube asset
            BlobPath = localPath,
            IsActive = true, // Default to active
            Tags = string.Empty // Can add logic to parse tags from YouTube if needed later
        };

        context.Assets.Add(newAsset);
        await context.SaveChangesAsync(cancellationToken);

        return newAsset.Id;
    }
}
