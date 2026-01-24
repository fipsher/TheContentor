namespace TheContentor.Infrastructure.Scrappers.Shared;

/// <summary>
/// Platform-agnostic request.
/// </summary>
public sealed record ScrapeRequest(
    IReadOnlyList<ScrapeTarget> Targets,
    ScrapeSort Sort = ScrapeSort.New,
    DateTimeOffset? UntilUtc = null,
    int MaxItemsPerTarget = 100,
    bool IncludeComments = false,
    int CommentLimit = 0
);