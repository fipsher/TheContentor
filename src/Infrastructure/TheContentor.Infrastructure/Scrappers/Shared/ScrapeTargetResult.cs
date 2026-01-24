namespace TheContentor.Infrastructure.Scrappers.Shared;

/// <summary>
/// A batch result for one target/community.
/// </summary>
public sealed record ScrapeTargetResult<TPost>(
    string Community,
    IReadOnlyList<TPost> Items,
    ScrapeCursor NewCursor,
    bool HasMore
);