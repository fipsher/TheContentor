using Reddit;

namespace TheContentor.Infrastructure.Scrappers.Reddit;

// r/pettyrevenge, r/ProRevenge, r/MaliciousCompliance, r/AmItheAsshole, r/JUSTNOMIL,r/raisedbynarcissists, r/entitledparents
public class RedditScrapper(RedditClient r)
{
    public void Scrape(string subredditName)
    {
        // Get info on another subreddit.
        var subreddit = r.Subreddit(subredditName).About();
        
        foreach (var post in subreddit.Posts.Hot)
        {
            // Some listings are infinite enumerables; you may want Take(n)
            var score = post.Score;                 // int
            var ratio = post.UpvoteRatio;           // double 0..1
            var ratioPct = ratio * 100.0;

            Console.WriteLine($"{score,6} | {ratioPct,5:0.0}% | {post.Title}");
        }
    }
}