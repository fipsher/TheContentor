using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class SourcePost : BaseEntity
{
    // Identity
    public SourcePlatform Platform { get; set; }
    public string ExternalId { get; set; } = string.Empty;   // Reddit thing id (t3_xxx) or post id
    public string ExternalUrl { get; set; } = string.Empty;

    // Where it came from
    public string Community { get; set; } = string.Empty;    // subreddit / group / forum name
    public string? CommunityExternalId { get; set; }         // optional
    public string? Flairs { get; set; }                      // normalized later or JSON list

    // Author (store minimal, don’t store PII beyond username)
    public string AuthorExternalId { get; set; } = string.Empty; // e.g. user id if available
    public string AuthorName { get; set; } = string.Empty;       // e.g. "throwaway123"

    // Content
    public string Title { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;       // selftext/body
    public string? RawHtml { get; set; }                      // optional
    public int WordCount { get; set; }
    public string Language { get; set; } = "en";

    // Engagement snapshot at ingest time (platform-agnostic)
    public int Score { get; set; }            // “net” score if available
    public int CommentCount { get; set; }
    public double? UpvoteRatio { get; set; }  // 0..1 when available
    public bool IsNsfw { get; set; }
    public bool IsSpoiler { get; set; }

    // Timing
    public DateTimeOffset CreatedUtc { get; set; }            // post created time on platform
    public DateTimeOffset IngestedUtc { get; set; }           // when you fetched it
    public DateTimeOffset? LastRefreshedUtc { get; set; }     // when you updated metrics

    // Processing state
    public SourcePostStatus Status { get; set; }
    public string? StatusReason { get; set; }                // why rejected/failed/etc

    // Dedup / integrity
    public string ContentHash { get; set; } = string.Empty;   // hash of normalized text to catch reposts
    public string MetadataJson { get; set; } = "{}";          // raw API payload (optional but useful)

    // Navigation
    public List<SourceComment> Comments { get; set; } = [];
    public List<PostMetricSnapshot> MetricSnapshots { get; set; } = [];
    public ProcessedPost? ProcessedPost { get; set; }
}
