using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.SourcePosts.Models;

/// <summary>Summary of a source post for list views.</summary>
public class SourcePostListItemDto
{
    /// <summary>Source post identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Origin platform for the post.</summary>
    public SourcePlatform Platform { get; set; }
    /// <summary>Community or subreddit name.</summary>
    public string Community { get; set; } = string.Empty;
    /// <summary>Post title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Word count of the raw text.</summary>
    public int WordCount { get; set; }
    /// <summary>Score reported by the platform.</summary>
    public int Score { get; set; }
    /// <summary>Upvote ratio when available.</summary>
    public double? UpvoteRatio { get; set; }
    /// <summary>Original creation time (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; set; }
    /// <summary>Ingestion time (UTC).</summary>
    public DateTimeOffset IngestedUtc { get; set; }
    /// <summary>Processing status.</summary>
    public SourcePostStatus Status { get; set; }
    /// <summary>External URL to the original post.</summary>
    public string ExternalUrl { get; set; } = string.Empty;
    /// <summary>TTS generation status from the linked processed post. Null if no processed post exists.</summary>
    public TtsStatus? TtsStatus { get; set; }
    /// <summary>Video generation status from the linked processed post. Null if no processed post exists.</summary>
    public VideoStatus? VideoStatus { get; set; }
    /// <summary>Whether the post has been marked as fully posted. Null if no processed post exists.</summary>
    public bool? IsPosted { get; set; }
}
