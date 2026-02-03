namespace TheContentor.Orchestrator.Models.Video;

public class VideoOrchestratorRequest
{
    public Guid ProcessedPostId { get; set; }
    public VideoSettingsDto Settings { get; set; } = null!;
}
