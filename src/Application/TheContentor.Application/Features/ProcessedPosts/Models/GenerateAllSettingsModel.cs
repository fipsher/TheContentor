namespace TheContentor.Application.Features.ProcessedPosts.Models;

/// <summary>Combined settings for TTS and video generation.</summary>
public class GenerateAllSettingsModel
{
    /// <summary>TTS voice, rate, and pitch settings.</summary>
    public TtsSettingsModel TtsSettings { get; set; } = new();
    /// <summary>Selected background video asset IDs.</summary>
    public List<Guid> AssetIds { get; set; } = [];
}
