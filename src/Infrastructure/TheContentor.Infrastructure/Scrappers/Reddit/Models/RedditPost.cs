namespace TheContentor.Infrastructure.Scrappers.Reddit.Models;

/// <summary>
/// Combined model for Reddit post data, containing both generic SourcePost info and Reddit-specific metadata.
/// This acts as the TPost for Reddit scraper.
/// </summary>
public sealed record RedditPost
{
    // Identity
    public string ExternalId { get; init; } = string.Empty;
    public Uri ExternalUrl { get; init; } = null!;

    // Where it came from
    public string Community { get; init; } = string.Empty;
    public string? CommunityExternalId { get; init; }
    public string? Flairs { get; init; }

    // Author
    public string AuthorExternalId { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;

    // Content
    public string Title { get; init; } = string.Empty;
    public string RawText { get; init; } = string.Empty;
    public string? RawHtml { get; init; }
    public int WordCount { get; init; }
    public string? Language { get; init; } = "en";

    // Engagement
    public int? Score { get; init; }
    public bool HideScore { get; init; }
    public int CommentCount { get; init; }
    public double? UpvoteRatio { get; init; }
    public bool IsNsfw { get; init; }
    public bool IsSpoiler { get; init; }

    // Timing
    public DateTimeOffset CreatedUtc { get; init; }

    // Reddit specifics
    public string Subreddit { get; init; } = string.Empty;
    public string Permalink { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsSelfPost { get; init; }
    public string? LinkUrl { get; init; }
    public string? Domain { get; init; }
    public string? FlairText { get; init; }

    // Author info available from Reddit
    public bool IsAuthorDeleted { get; init; }
    public DateTimeOffset? AuthorCreatedUtc { get; init; }
    public int? AuthorLinkKarma { get; init; }
    public int? AuthorCommentKarma { get; init; }

    // Moderation-ish flags
    public bool IsLocked { get; init; }
    public bool IsRemoved { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsStickied { get; init; }
    public bool IsArchived { get; init; }

    // Awards / other signals
    public int? TotalAwardsReceived { get; init; }
    
    // Comments
    public List<RedditComment> Comments { get; init; } = [];
    
    // Raw metadata
    public string MetadataJson { get; init; } = "{}";
}
