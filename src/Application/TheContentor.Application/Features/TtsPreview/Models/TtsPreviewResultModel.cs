namespace TheContentor.Application.Features.TtsPreview.Models;

/// <summary>Result of a TTS preview generation.</summary>
public class TtsPreviewResultModel
{
    /// <summary>Relative storage path served at /storage/{AudioPath}.</summary>
    public string AudioPath { get; set; } = string.Empty;

    /// <summary>Audio duration in seconds.</summary>
    public double DurationSeconds { get; set; }
}