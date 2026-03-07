namespace TheContentor.Orchestrator.Models.GenerateWeek;

/// <summary>Request to trigger the generate-week orchestration for bulk processing.</summary>
public class GenerateWeekOrchestratorRequest
{
    /// <summary>Week start date (ISO format).</summary>
    public string WeekStart { get; set; } = string.Empty;
    /// <summary>Ordered list of posts to process.</summary>
    public List<WeekPostItem> Items { get; set; } = [];
}

/// <summary>A single post within the generate-week batch.</summary>
public class WeekPostItem
{
    /// <summary>Source post identifier.</summary>
    public Guid SourcePostId { get; set; }
    /// <summary>Processed post identifier. Null if AI processing is needed first.</summary>
    public Guid? ProcessedPostId { get; set; }
    /// <summary>Whether this post needs AI processing before video generation.</summary>
    public bool NeedsAiProcessing { get; set; }
    /// <summary>Background video asset identifier.</summary>
    public Guid AssetId { get; set; }
    /// <summary>Kokoro voice identifier.</summary>
    public string Voice { get; set; } = string.Empty;
    /// <summary>Narrator gender ("Male" or "Female").</summary>
    public string NarratorGender { get; set; } = "Male";
}

/// <summary>Progress update for the generate-week orchestration.</summary>
public class GenerateWeekProgressDto
{
    /// <summary>Week start date.</summary>
    public string WeekStart { get; set; } = string.Empty;
    /// <summary>Total number of posts in the batch.</summary>
    public int TotalPosts { get; set; }
    /// <summary>Number of posts completed so far.</summary>
    public int CompletedPosts { get; set; }
    /// <summary>Source post ID currently being processed.</summary>
    public Guid? CurrentSourcePostId { get; set; }
    /// <summary>Current stage description.</summary>
    public string Stage { get; set; } = string.Empty;
    /// <summary>True when all posts are done.</summary>
    public bool IsComplete { get; set; }
    /// <summary>True if any post failed.</summary>
    public bool HasError { get; set; }
    /// <summary>Error message if HasError.</summary>
    public string? ErrorMessage { get; set; }
}
