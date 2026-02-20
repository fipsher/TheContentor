using TheContentor.Orchestrator.Models.TTS;

namespace TheContentor.Orchestrator.Models.GenerateAll;

/// <summary>Request to trigger the unified TTS + Video orchestration.</summary>
public class GenerateAllOrchestratorRequest
{
    /// <summary>ProcessedPost identifier.</summary>
    public Guid ProcessedPostId { get; set; }
    /// <summary>TTS voice, rate, and pitch settings.</summary>
    public TtsSettingsDto TtsSettings { get; set; } = null!;
    /// <summary>Selected background video asset IDs.</summary>
    public List<Guid> AssetIds { get; set; } = [];
}
