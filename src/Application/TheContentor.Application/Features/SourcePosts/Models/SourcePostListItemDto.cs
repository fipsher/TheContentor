using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.SourcePosts.Models;

/// <summary>
/// Represents a summary of a source post for list views.
/// </summary>
public class SourcePostListItemDto
{
    public Guid Id { get; set; }
    public SourcePlatform Platform { get; set; }
    public string Community { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int Score { get; set; }
    public double? UpvoteRatio { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset IngestedUtc { get; set; }
    public SourcePostStatus Status { get; set; }
    public string ExternalUrl { get; set; } = string.Empty;
}
