using System.Text.Json;
using System.Web;
using RestSharp;
using TheContentor.Infrastructure.Scrappers.Reddit.Models;
using TheContentor.Infrastructure.Scrappers.Shared;

namespace TheContentor.Infrastructure.Scrappers.Reddit;

// r/pettyrevenge, r/ProRevenge, r/MaliciousCompliance, r/AmItheAsshole, r/JUSTNOMIL,r/raisedbynarcissists, r/entitledparents
/// <summary>
/// Reddit implementation of the source scraper.
/// </summary>
public class RedditScrapper(IRestClient client) : ISourceScraper<RedditPost, RedditScrapperRequest>
{
    private static readonly Uri RedditUri = new("https://www.reddit.com/");

    public async Task<IEnumerable<RedditPost>> ScrapeListAsync(RedditScrapperRequest request)
    {
        var uri = new Uri(RedditUri, $"r/{request.Subreddit}/{request.RedditSort.ToString().ToLowerInvariant()}.json");

        var queryString = HttpUtility.ParseQueryString(string.Empty);
        if (request.Limit.HasValue)
        {
            queryString.Add("limit", request.Limit.ToString());
        }

        if (!string.IsNullOrWhiteSpace(request.After))
        {
            queryString.Add("after", request.After);
        }

        var ub = new UriBuilder(uri)
        {
            Query = queryString.ToString(),
        };

        var response = await client.ExecuteGetAsync<RedditNestedJson>(ub.Uri.ToString());

        if (response.Data?.Data?.Children == null)
        {
            return [];
        }

        return response.Data.Data.Children
            .Where(c => c.Kind == "t3") // t3 is Post
            .Select(c => MapToRedditPost(c.Data));
    }

    public async Task<RedditPost> ScrapeItemAsync(RedditPost post, int? depth = null)
    {
        var uri = new Uri(RedditUri, $"{post.Permalink.TrimStart('/')}.json");

        var queryString = HttpUtility.ParseQueryString(string.Empty);
        if (depth.HasValue)
        {
            queryString.Add("depth", depth.ToString());
        }

        var ub = new UriBuilder(uri)
        {
            Query = queryString.ToString(),
        };

        var response = await client.ExecuteGetAsync<List<RedditNestedJson>>(ub.Uri.ToString());

        if (response.Data == null || response.Data.Count == 0)
        {
            return post;
        }

        // The first element in the array is the post itself
        var postListing = response.Data[0];
        if (postListing.Data?.Children is { Count: > 0 })
        {
            var updatedPost = MapToRedditPost(postListing.Data.Children[0].Data);

            // The second element in the array contains the comments
            if (response.Data.Count > 1)
            {
                var commentsListing = response.Data[1];
                var comments = ExtractComments(commentsListing);
                updatedPost = updatedPost with { Comments = comments };
            }

            return updatedPost;
        }

        return post;
    }

    private static List<RedditComment> ExtractComments(RedditNestedJson? listing)
    {
        if (listing?.Data?.Children == null)
        {
            return [];
        }

        var comments = new List<RedditComment>();
        foreach (var child in listing.Data.Children.Where(c => c.Kind == "t1"))
        {
            var comment = MapToRedditComment(child.Data);
            
            // Handle replies
            if (child.Data.Replies is JsonElement repliesElement && repliesElement.ValueKind == JsonValueKind.Object)
            {
                var repliesListing = repliesElement.Deserialize<RedditNestedJson>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (repliesListing != null)
                {
                    comment = comment with { Replies = ExtractComments(repliesListing) };
                }
            }

            comments.Add(comment);
        }

        return comments;
    }

    private static RedditComment MapToRedditComment(RedditListItem item)
    {
        return new RedditComment
        {
            ExternalId = item.Id ?? string.Empty,
            AuthorName = item.Author ?? string.Empty,
            Body = item.Body ?? string.Empty,
            BodyHtml = item.BodyHtml,
            Score = item.Score,
            HideScore = item.HideScore,
            CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)item.CreatedUtc),
            Permalink = item.Permalink ?? string.Empty,
            FullName = item.Name ?? string.Empty,
            MetadataJson = JsonSerializer.Serialize(item)
        };
    }

    private static RedditPost MapToRedditPost(RedditListItem item)
    {
        return new RedditPost
        {
            ExternalId = item.Id ?? string.Empty,
            ExternalUrl = !string.IsNullOrEmpty(item.Url) ? new Uri(item.Url) : null!,
            Community = item.Subreddit ?? string.Empty,
            CommunityExternalId = item.SubredditId,
            Flairs = item.LinkFlairText,
            AuthorExternalId = item.AuthorFullname ?? string.Empty,
            AuthorName = item.Author ?? string.Empty,
            Title = item.Title ?? string.Empty,
            RawText = item.SelfText ?? string.Empty,
            RawHtml = item.SelfTextHtml,
            WordCount = (item.SelfText ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            Language = null,
            Score = item.Score,
            HideScore = item.HideScore,
            CommentCount = item.NumComments,
            UpvoteRatio = item.UpvoteRatio,
            IsNsfw = item.Over18,
            IsSpoiler = item.Spoiler,
            CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)item.CreatedUtc),
            Subreddit = item.Subreddit ?? string.Empty,
            Permalink = item.Permalink ?? string.Empty,
            FullName = item.Name ?? string.Empty,
            IsSelfPost = item.IsSelf,
            LinkUrl = item.Url,
            Domain = item.Domain,
            FlairText = item.LinkFlairText,
            IsLocked = item.Locked,
            IsArchived = item.Archived,
            IsStickied = item.Stickied,
            TotalAwardsReceived = item.TotalAwardsReceived,
            MetadataJson = JsonSerializer.Serialize(item)
        };
    }
}
