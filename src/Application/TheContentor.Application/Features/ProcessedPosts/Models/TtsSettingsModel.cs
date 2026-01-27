namespace TheContentor.Application.Features.ProcessedPosts.Models;

/// <summary>
/// TTS settings for voice generation
/// </summary>
public class TtsSettingsModel
{
    public string Voice { get; set; } = string.Empty;
    public int Rate { get; set; } = 0; // -50 to +50 percentage
    public int Pitch { get; set; } = 0; // -20 to +20 Hz
}
