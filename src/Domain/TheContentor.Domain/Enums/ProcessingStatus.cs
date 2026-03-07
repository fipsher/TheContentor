namespace TheContentor.Domain.Enums;

public enum SourcePostStatus
{
    Raw = 1,
    Processed = 2,
    Approved = 3,
    Rejected = 4,
    Skipped = 5,
    /// <summary>AI processing is queued or in progress.</summary>
    Processing = 6,
}
