namespace TheContentor.Infrastructure.Scrappers.Shared;

/// <summary>
/// Per-community checkpoint. Supports both time-based progress and token/id-based pagination.
/// </summary>
public sealed record ScrapeCursor(
    DateTimeOffset? WatermarkUtc = null,    // "I have scraped everything <= this created time"
    string? LastExternalId = null,          // tie-breaker when same timestamp
    string? ContinuationToken = null        // pagination token if platform uses it
);