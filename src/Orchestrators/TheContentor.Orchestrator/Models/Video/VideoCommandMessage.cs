using TheContentor.Orchestrator.Models.TTS;

namespace TheContentor.Orchestrator.Models.Video;

public class VideoCommandMessage
{
    public string CommandType { get; set; } = string.Empty; // "concat-cut", "generate-subtitles", "compose"
    public Guid ProcessedPostId { get; set; }
    public Guid? PartId { get; set; }
    public string OrchestrationInstanceId { get; set; } = string.Empty;

    // For concat-cut
    public List<BlobPathInfo>? AssetBlobPaths { get; set; }
    public TimeSpan? TargetDuration { get; set; }
    public TimeSpan? VideoOffset { get; set; }

    // For generate-subtitles
    public BlobPathInfo? AudioBlobPath { get; set; }

    // For compose
    public BlobPathInfo? VideoBlobPath { get; set; }
    public BlobPathInfo? SubtitleBlobPath { get; set; }
}
