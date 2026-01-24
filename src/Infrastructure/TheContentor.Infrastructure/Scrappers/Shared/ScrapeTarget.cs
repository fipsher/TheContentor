namespace TheContentor.Infrastructure.Scrappers.Shared;

/// <summary>
/// A community to scrape + its cursor (progress).
/// For Reddit, Community = subreddit name.
/// For other platforms, could be groupId/channelId/forum section, etc.
/// </summary>
public sealed record ScrapeTarget(
    string Community,
    ScrapeCursor Cursor
);