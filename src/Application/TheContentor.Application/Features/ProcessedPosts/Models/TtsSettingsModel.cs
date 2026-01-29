namespace TheContentor.Application.Features.ProcessedPosts.Models;

/// <summary>Text-to-speech settings for voice generation.</summary>
public class TtsSettingsModel
{
    /// <summary>Voice identifier or preset name.</summary>
    public string Voice { get; set; } = string.Empty;
    /// <summary>Speech rate adjustment percentage (-50 to 50).</summary>
    public int Rate { get; set; } = 0;
    /// <summary>Pitch adjustment in Hz (-20 to 20).</summary>
    public int Pitch { get; set; } = 0;
}
