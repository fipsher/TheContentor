using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.SourcePosts.Models;

/// <summary>
/// Represents full details of a source post.
/// </summary>
public class SourcePostDetailsDto
{
    public Guid Id { get; set; }
    public SourcePlatform Platform { get; set; }
    public string Community { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int Score { get; set; }
    public double? UpvoteRatio { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public string ExternalUrl { get; set; } = string.Empty;
    public SourcePostStatus Status { get; set; }
    public ProcessedPostDto? ProcessedPost { get; set; }
}

public class ProcessedPostDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
    public List<ProcessedPostPartDto> Parts { get; set; } = new();
}

public class ProcessedPostPartDto
{
    public Guid Id { get; set; }
    public string ProcessedText { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
    public List<SocialPlatform> PublishedTo { get; set; } = new();
    public int Part { get; set; }
}
