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
public class RedditScrapper(RestClient client) : ISourceScraper<RedditPost, RedditScrapperRequest>
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

        if (response.Data?.Data?.children == null)
        {
            return [];
        }

        return response.Data.Data.children
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
        if (postListing.Data?.children is { Count: > 0 })
        {
            var updatedPost = MapToRedditPost(postListing.Data.children[0].Data);

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
        if (listing?.Data?.children == null)
        {
            return [];
        }

        var comments = new List<RedditComment>();
        foreach (var child in listing.Data.children.Where(c => c.Kind == "t1"))
        {
            var comment = MapToRedditComment(child.Data);
            
            // Handle replies
            if (child.Data.replies is JsonElement repliesElement && repliesElement.ValueKind == JsonValueKind.Object)
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
            ExternalId = item.id ?? string.Empty,
            AuthorName = item.author ?? string.Empty,
            Body = item.body ?? string.Empty,
            BodyHtml = item.body_html,
            Score = item.score,
            HideScore = item.hide_score,
            CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)item.created_utc),
            Permalink = item.permalink ?? string.Empty,
            FullName = item.name ?? string.Empty,
            MetadataJson = JsonSerializer.Serialize(item)
        };
    }

    private static RedditPost MapToRedditPost(RedditListItem item)
    {
        return new RedditPost
        {
            ExternalId = item.id ?? string.Empty,
            ExternalUrl = !string.IsNullOrEmpty(item.url) ? new Uri(item.url) : null!,
            Community = item.subreddit ?? string.Empty,
            CommunityExternalId = item.subreddit_id,
            Flairs = item.link_flair_text,
            AuthorExternalId = item.author_fullname ?? string.Empty,
            AuthorName = item.author ?? string.Empty,
            Title = item.title ?? string.Empty,
            RawText = item.selftext ?? string.Empty,
            RawHtml = item.selftext_html,
            WordCount = (item.selftext ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            Language = null,
            Score = item.score,
            HideScore = item.hide_score,
            CommentCount = item.num_comments,
            UpvoteRatio = item.upvote_ratio,
            IsNsfw = item.over_18,
            IsSpoiler = item.spoiler,
            CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)item.created_utc),
            Subreddit = item.subreddit ?? string.Empty,
            Permalink = item.permalink ?? string.Empty,
            FullName = item.name ?? string.Empty,
            IsSelfPost = item.is_self,
            LinkUrl = item.url,
            Domain = item.domain,
            FlairText = item.link_flair_text,
            IsLocked = item.locked,
            IsArchived = item.archived,
            IsStickied = item.stickied,
            TotalAwardsReceived = item.total_awards_received,
            MetadataJson = JsonSerializer.Serialize(item)
        };
    }
}