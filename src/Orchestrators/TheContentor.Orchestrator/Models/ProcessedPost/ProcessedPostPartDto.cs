namespace TheContentor.Orchestrator.Models.ProcessedPost;

public class ProcessedPostPartDto
{
    /// <summary>Processed part identifier.</summary>
    public Guid? Id { get; set; }
    /// <summary>Processed narration text.</summary>
    public string ProcessedText { get; set; } = string.Empty;
    /// <summary>Sequence number for the part.</summary>
    public int Part { get; set; }
    /// <summary>Audio container name.</summary>
    public string AudioContainer { get; set; } = string.Empty;
    /// <summary>Audio path in container.</summary>
    public string AudioPath { get; set; } = string.Empty;
}
