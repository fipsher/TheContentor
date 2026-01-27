namespace TheContentor.Orchestrator.Models.TTS;

/// <summary>
/// Request to trigger TTS orchestration
/// </summary>
public class TtsOrchestratorRequest
{
    public Guid ProcessedPostId { get; set; }
    public TtsSettingsDto Settings { get; set; } = null!;
}