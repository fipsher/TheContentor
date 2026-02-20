namespace TheContentor.Orchestrator.Models.TTS;

/// <summary>
/// Event callback from TTS worker
/// </summary>
public class TtsEventCallback
{
    public string OrchestrationInstanceId { get; set; } = string.Empty;
    public Guid ProcessedPostId { get; set; }
    public Guid? PartId { get; set; }
    public string? BlobContainer { get; set; }
    public string? BlobPath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string TextType { get; set; } = string.Empty; // "description" or "part"
    public double? AudioDurationSeconds { get; set; }
}