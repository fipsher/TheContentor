namespace TheContentor.Domain.Enums;

/// <summary>
/// Status of video generation for a ProcessedPost
/// </summary>
public enum VideoStatus
{
    NotGenerated = 1,
    InProgress = 2,
    Generated = 3,
    Failed = 4
}
