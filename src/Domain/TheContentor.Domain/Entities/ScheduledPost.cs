using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

/// <summary>Associates a source post with a specific publication calendar day.</summary>
public class ScheduledPost : BaseEntity
{
    /// <summary>The calendar day on which the post is scheduled.</summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>Identifier of the source post assigned to this day.</summary>
    public Guid SourcePostId { get; set; }

    /// <summary>The source post assigned to this day.</summary>
    public SourcePost SourcePost { get; set; } = null!;

    /// <summary>UTC timestamp of when this schedule entry was created.</summary>
    public DateTimeOffset CreatedUtc { get; set; }
}
