namespace TheContentor.Orchestrator.Models.TTS;

/// <summary>
/// Command message for TTS worker
/// </summary>
public class TtsCommandMessage
{
    public string Text { get; set; } = string.Empty;
    public string Voice { get; set; } = string.Empty;
    public int Rate { get; set; }
    public int Pitch { get; set; }
    public Guid ProcessedPostId { get; set; }
    public Guid? PartId { get; set; }
    public string OrchestrationInstanceId { get; set; } = string.Empty;
    public string TextType { get; set; } = string.Empty; // "description" or "part"
    /// <summary>TTS engine name ("EdgeTTS" or "Kokoro").</summary>
    public string Engine { get; set; } = "EdgeTTS";
}