namespace TheContentor.Application.Features.Schedule.Models;

/// <summary>Represents a single scheduled calendar day with its associated post summary.</summary>
public class ScheduledDayDto
{
    /// <summary>The schedule entry identifier.</summary>
    public Guid ScheduledPostId { get; set; }

    /// <summary>The calendar day of this entry.</summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>Identifier of the assigned source post.</summary>
    public Guid SourcePostId { get; set; }

    /// <summary>Title of the source post.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Community (subreddit) of the source post.</summary>
    public string Community { get; set; } = string.Empty;

    /// <summary>Score of the source post.</summary>
    public int Score { get; set; }

    /// <summary>Whether the source post has been processed into a <c>ProcessedPost</c>.</summary>
    public bool IsProcessed { get; set; }

    /// <summary>Whether the post has already been published. Null when not yet processed.</summary>
    public bool? IsPosted { get; set; }

    /// <summary>When this schedule entry was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; set; }
}
