using MediatR;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Queries.GetYouTubeVideoMetadata;

/// <summary>Fetches title and available quality tiers for a YouTube video before upload.</summary>
public record GetYouTubeVideoMetadataQuery(string YouTubeUrl) : IRequest<YouTubeVideoMetadataDto>;

/// <summary>Calls YouTube services to resolve video title and available qualities.</summary>
public class GetYouTubeVideoMetadataQueryHandler(IYouTubeService youtubeService)
    : IRequestHandler<GetYouTubeVideoMetadataQuery, YouTubeVideoMetadataDto>
{
    /// <summary>Returns video title and available quality options, or throws if the URL is invalid.</summary>
    public async Task<YouTubeVideoMetadataDto> Handle(GetYouTubeVideoMetadataQuery request, CancellationToken cancellationToken)
    {
        if (!await youtubeService.IsValidYouTubeUrlAsync(request.YouTubeUrl))
            throw new ArgumentException("Invalid YouTube URL provided.", nameof(request.YouTubeUrl));

        var (metadataTask, qualitiesTask) = (
            youtubeService.GetVideoMetadataAsync(request.YouTubeUrl),
            youtubeService.GetAvailableQualitiesAsync(request.YouTubeUrl)
        );

        await Task.WhenAll(metadataTask, qualitiesTask);

        var metadata = await metadataTask
            ?? throw new InvalidOperationException("Could not retrieve video metadata.");

        return new YouTubeVideoMetadataDto(metadata.Title, await qualitiesTask);
    }
}
