namespace TheContentor.Infrastructure.Scrappers.Shared;

/// <summary>
/// One "scrape run" result - one entry per target.
/// </summary>
public sealed record ScrapeResult<TPost>(
    IReadOnlyList<ScrapeTargetResult<TPost>> Results
);