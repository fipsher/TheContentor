namespace TheContentor.Infrastructure.Scrappers.Shared;

public interface ISourceScraper<TPost, in TRequest>
{
    public Task<IEnumerable<TPost>> ScrapeListAsync(TRequest request);
    public Task<TPost> ScrapeItemAsync(TPost request, int? depth = null);
}