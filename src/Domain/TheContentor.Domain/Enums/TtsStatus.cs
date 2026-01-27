namespace TheContentor.Domain.Enums;

/// <summary>
/// Status of TTS generation for a ProcessedPost
/// </summary>
public enum TtsStatus
{
    NotGenerated = 1,
    InProgress = 2,
    Generated = 3,
    Failed = 4
}
