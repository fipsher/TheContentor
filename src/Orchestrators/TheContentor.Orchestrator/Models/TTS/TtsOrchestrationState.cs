namespace TheContentor.Orchestrator.Models.TTS;

/// <summary>
/// State for TTS orchestration
/// </summary>
public class TtsOrchestrationState
{
    public Guid ProcessedPostId { get; set; }
    public int ExpectedCallbacks { get; set; }
    public int ReceivedCallbacks { get; set; }
    public Dictionary<string, BlobPathInfo> CompletedItems { get; set; } = new();
    public bool HasErrors { get; set; }
}