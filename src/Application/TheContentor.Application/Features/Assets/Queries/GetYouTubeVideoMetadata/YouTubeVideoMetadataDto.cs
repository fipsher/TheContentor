using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.Assets.Queries.GetYouTubeVideoMetadata;

/// <summary>YouTube video metadata returned before upload so the user can confirm name and quality.</summary>
public record YouTubeVideoMetadataDto(string Title, IReadOnlyList<YouTubeVideoQuality> AvailableQualities);
