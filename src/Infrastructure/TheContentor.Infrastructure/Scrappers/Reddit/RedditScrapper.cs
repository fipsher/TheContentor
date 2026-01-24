using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure.Scrappers.Reddit.Models;
using TheContentor.Infrastructure.Scrappers.Shared;

namespace TheContentor.Infrastructure.Scrappers.Reddit;

// r/pettyrevenge, r/ProRevenge, r/MaliciousCompliance, r/AmItheAsshole, r/JUSTNOMIL,r/raisedbynarcissists, r/entitledparents
/// <summary>
/// Reddit implementation of the source scraper.
/// Uses Reddit.NET library to fetch posts from subreddits.
/// </summary>
public class RedditScrapper(RedditClient redditClient) : ISourceScraper<RedditPost>
{
    public SourcePlatform Platform => SourcePlatform.Reddit;

    /// <inheritdoc />
    public async Task<ScrapeResult<RedditPost>> ScrapeAsync(
        ScrapeRequest request,
        CancellationToken ct = default)
    {
        var targetResults = new List<ScrapeTargetResult<RedditPost>>();

        foreach (var target in request.Targets)
        {
            if (await FetchAndProcessRedditPosts(request, ct, target, targetResults)) break;
        }

        return new ScrapeResult<RedditPost>(targetResults);
    }

    private async Task<bool> FetchAndProcessRedditPosts(
        ScrapeRequest request,
        CancellationToken ct,
        ScrapeTarget target,
        List<ScrapeTargetResult<RedditPost>> targetResults)
    {
        if (ct.IsCancellationRequested) return true;

        var subreddit = redditClient.Subreddit(target.Community);

        // Fetch posts from Reddit API. Wrapping in Task.Run since Reddit.NET is largely synchronous.
        var rawPosts =
            await Task.Run(() => FetchRawPosts(subreddit, request.Sort, target.Cursor, request.MaxItemsPerTarget), ct);

        var items = new List<RedditPost>();
        foreach (var post in rawPosts)
        {
            if (ct.IsCancellationRequested) break;

            // Time-based filtering
            if (request.UntilUtc.HasValue && post.Created < request.UntilUtc.Value.UtcDateTime)
            {
                if (request.Sort == ScrapeSort.New)
                {
                    // For 'New' sort, we can stop early as subsequent posts will be even older.
                    break;
                }

                continue;
            }

            items.Add(MapToRedditPost(post));
        }

        var lastPost = items.LastOrDefault();
        var newCursor = new ScrapeCursor(
            WatermarkUtc: lastPost?.CreatedUtc,
            LastExternalId: lastPost?.FullName,
            ContinuationToken: lastPost?.FullName
        );

        // If we fetched the limit, there might be more items.
        var hasMore = rawPosts.Count >= request.MaxItemsPerTarget;

        targetResults.Add(new ScrapeTargetResult<RedditPost>(
            target.Community,
            items,
            newCursor,
            hasMore
        ));
        return false;
    }

    private List<Post> FetchRawPosts(Subreddit subreddit, ScrapeSort sort, ScrapeCursor cursor, int limit)
    {
        var after = cursor.ContinuationToken ?? cursor.LastExternalId;

        return sort switch
        {
            ScrapeSort.New => subreddit.Posts.GetNew(after: after, limit: limit),
            ScrapeSort.Top => subreddit.Posts.GetTop(t: "all", after: after, limit: limit),
            ScrapeSort.Hot => subreddit.Posts.GetHot(after: after, limit: limit),
            _ => subreddit.Posts.GetHot(after: after, limit: limit)
        };
    }

    private RedditPost MapToRedditPost(Post post)
    {
        var isSelfPost = post is SelfPost;
        var selfText = isSelfPost ? ((SelfPost)post).SelfText : string.Empty;
        var linkUrl = !isSelfPost ? ((LinkPost)post).URL : string.Empty;

        return new RedditPost
        {
            ExternalId = post.Id,
            ExternalUrl = "https://reddit.com" + post.Permalink,
            Community = post.Subreddit,
            CommunityExternalId = null, // Often not directly exposed on the post object without an extra call

            AuthorExternalId = post.Author,
            AuthorName = post.Author,

            Title = post.Title,
            RawText = isSelfPost ? selfText : linkUrl,
            WordCount = CountWords(isSelfPost ? selfText : string.Empty),
            Language = "en",

            Score = post.Score,
            CommentCount = post.Listing.NumComments,
            UpvoteRatio = post.UpvoteRatio,
            IsNsfw = post.Listing.Over18,
            IsSpoiler = post.Listing.Spoiler,

            CreatedUtc = new DateTimeOffset(post.Created, TimeSpan.Zero),

            Subreddit = post.Subreddit,
            Permalink = post.Permalink,
            FullName = post.Fullname,
            IsSelfPost = isSelfPost,
            LinkUrl = isSelfPost ? null : linkUrl,
            Domain = post.Listing.Domain,
            FlairText = post.Listing.LinkFlairText,

            IsAuthorDeleted = post.Author == "[deleted]",

            // These flags are typically available in the underlying Thing data
            IsLocked = post.Listing.Locked,
            IsRemoved = false,
            IsDeleted = post.Author == "[deleted]",
            IsStickied = post.Listing.Stickied,
            IsArchived = post.Listing.Archived,

            TotalAwardsReceived = null,
            MetadataJson = JsonConvert.SerializeObject(post.Listing)
        };
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
    }
}