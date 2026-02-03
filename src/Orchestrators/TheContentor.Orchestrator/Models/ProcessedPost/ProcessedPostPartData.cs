namespace TheContentor.Orchestrator.Models.ProcessedPost;

public class ProcessedPostPartData
{
    /// <summary>Processed part identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Processed narration text.</summary>
    public string ProcessedText { get; set; } = string.Empty;
    /// <summary>Sequence number for the part.</summary>
    public int Part { get; set; }
}
