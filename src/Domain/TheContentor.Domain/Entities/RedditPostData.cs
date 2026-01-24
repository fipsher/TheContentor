using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class RedditPostData: BaseEntity
{
    public SourcePost SourcePost { get; set; } = null!;

    // Reddit specifics
    public string Subreddit { get; set; } = string.Empty;     // redundant but convenient
    public string Permalink { get; set; } = string.Empty;     // /r/.../comments/...
    public string FullName { get; set; } = string.Empty;      // t3_xxx
    public bool IsSelfPost { get; set; }
    public string? LinkUrl { get; set; }                      // if link post
    public string? Domain { get; set; }
    public string? FlairText { get; set; }

    // Author info available from Reddit
    public bool IsAuthorDeleted { get; set; }
    public DateTimeOffset? AuthorCreatedUtc { get; set; }     // if you fetch user info
    public int? AuthorLinkKarma { get; set; }
    public int? AuthorCommentKarma { get; set; }

    // Moderation-ish flags
    public bool IsLocked { get; set; }
    public bool IsRemoved { get; set; }       // removed by mods
    public bool IsDeleted { get; set; }       // deleted by user
    public bool IsStickied { get; set; }
    public bool IsArchived { get; set; }

    // Awards / other signals (optional)
    public int? TotalAwardsReceived { get; set; }
}