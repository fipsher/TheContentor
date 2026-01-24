using TheContentor.Infrastructure.Scrappers.Shared;

namespace TheContentor.Infrastructure.Scrappers.Reddit.Models;

public class RedditNestedJson
{
    public string? Kind { get; set; }
    public RedditListItem Data { get; set; }
}