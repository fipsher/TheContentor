using TheContentor.Orchestrator.Models.TTS;

namespace TheContentor.Orchestrator.Models.Video;

public class VideoOrchestrationState
{
    public Guid ProcessedPostId { get; set; }
    public bool HasErrors { get; set; }
    public int ExpectedCallbacks { get; set; }
    public int ReceivedCallbacks { get; set; }
    public Dictionary<string, BlobPathInfo> CompletedItems { get; set; } = new();
}
