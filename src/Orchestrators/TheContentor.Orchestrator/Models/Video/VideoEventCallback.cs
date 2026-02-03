namespace TheContentor.Orchestrator.Models.Video;

public class VideoEventCallback
{
    public string OrchestrationInstanceId { get; set; } = string.Empty;
    public Guid ProcessedPostId { get; set; }
    public Guid? PartId { get; set; }
    public string CommandType { get; set; } = string.Empty; // "concat-cut", "generate-subtitles", "compose"
    public string? BlobContainer { get; set; }
    public string? BlobPath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration { get; set; } // For video duration
}
