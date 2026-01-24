using System.Text.Json;

namespace TheContentor.Infrastructure.Scrappers.Reddit.Models;

public sealed record RedditComment
{
    public string ExternalId { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? BodyHtml { get; init; }
    public bool HideScore { get; init; }
    public int? Score { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
    public string Permalink { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public int? RepliesCount { get; init; }
    
    // Raw metadata
    public string MetadataJson { get; init; } = "{}";
    
    // Potentially nested replies
    public List<RedditComment> Replies { get; init; } = new();
}
