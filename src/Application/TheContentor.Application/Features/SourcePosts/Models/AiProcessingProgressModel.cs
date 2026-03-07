namespace TheContentor.Application.Features.SourcePosts.Models;

/// <summary>SignalR payload for AI processing progress, keyed by SourcePostId.</summary>
public class AiProcessingProgressModel
{
    /// <summary>Source post identifier.</summary>
    public Guid SourcePostId { get; set; }
    /// <summary>Current stage label.</summary>
    public string Stage { get; set; } = string.Empty;
    /// <summary>Overall progress percentage (0-100).</summary>
    public int ProgressPercent { get; set; }
    /// <summary>Whether processing has finished (success or failure).</summary>
    public bool IsComplete { get; set; }
    /// <summary>Whether processing ended in error.</summary>
    public bool HasError { get; set; }
    /// <summary>Error details when HasError is true.</summary>
    public string? ErrorMessage { get; set; }
}
