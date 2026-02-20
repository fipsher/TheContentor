namespace TheContentor.Application.Features.ProcessedPosts.Models;

/// <summary>Progress update for the generation pipeline.</summary>
public class GenerationProgressModel
{
    /// <summary>ProcessedPost identifier.</summary>
    public Guid ProcessedPostId { get; set; }
    /// <summary>Overall progress percentage (0-100).</summary>
    public int ProgressPercent { get; set; }
    /// <summary>Current stage label for display.</summary>
    public string Stage { get; set; } = string.Empty;
    /// <summary>Human-readable detail message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>True when the pipeline has finished (success or failure).</summary>
    public bool IsComplete { get; set; }
    /// <summary>True if the pipeline ended in error.</summary>
    public bool HasError { get; set; }
    /// <summary>Error message when HasError is true.</summary>
    public string? ErrorMessage { get; set; }
}
