using TheContentor.Orchestrator.Models.TTS;

namespace TheContentor.Orchestrator.Models.Video;

/// <summary>Mutable state carried through the video orchestration workflow.</summary>
public class VideoOrchestrationState
{
    /// <summary>Identifies the processed post being orchestrated.</summary>
    public Guid ProcessedPostId { get; set; }
    /// <summary>Indicates whether any step has reported a failure.</summary>
    public bool HasErrors { get; set; }
    /// <summary>Total number of video callbacks expected before the orchestration finishes.</summary>
    public int ExpectedCallbacks { get; set; }
    /// <summary>Number of video callbacks received so far.</summary>
    public int ReceivedCallbacks { get; set; }
    /// <summary>Final composed video blob paths, keyed by a string identifier.</summary>
    public Dictionary<string, BlobPathInfo> CompletedItems { get; set; } = new();
    /// <summary>Intermediate concat-cut video blob paths, keyed by part ID.</summary>
    public Dictionary<Guid, BlobPathInfo> IntermediateVideos { get; set; } = new();
    /// <summary>Intermediate subtitle JSON blob paths, keyed by part ID.</summary>
    public Dictionary<Guid, BlobPathInfo> IntermediateSubtitles { get; set; } = new();
}
