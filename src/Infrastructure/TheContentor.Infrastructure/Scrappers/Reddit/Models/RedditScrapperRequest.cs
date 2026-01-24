namespace TheContentor.Infrastructure.Scrappers.Reddit.Models;

public class RedditScrapperRequest
{
    public required string Subreddit { get; set; }
    public required RedditSort RedditSort { get; set; }
    
    public int? Limit { get; set; }
    public int? Depth { get; set; }
    public string? After { get; set; }
}

public enum RedditSort
{
    Best = 1,
    Hot = 2,
    New = 3,
    Top = 4,
    Rising = 5
}