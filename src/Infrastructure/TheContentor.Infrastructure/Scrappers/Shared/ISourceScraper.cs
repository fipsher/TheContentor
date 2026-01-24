using TheContentor.Domain.Enums;

namespace TheContentor.Infrastructure.Scrappers.Shared;

public interface ISourceScraper<TPost>
{
    SourcePlatform Platform { get; }

    /// <summary>
    /// Scrape each target once (one batch per community).
    /// Caller controls scheduling + repeated calls if HasMore is true.
    /// </summary>
    Task<ScrapeResult<TPost>> ScrapeAsync(
        ScrapeRequest request,
        CancellationToken ct = default
    );
}