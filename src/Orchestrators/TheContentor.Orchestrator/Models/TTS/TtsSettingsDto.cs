namespace TheContentor.Orchestrator.Models.TTS;

/// <summary>
/// TTS settings
/// </summary>
public class TtsSettingsDto
{
    public string Voice { get; set; } = string.Empty;
    public int Rate { get; set; }
    public int Pitch { get; set; }
}