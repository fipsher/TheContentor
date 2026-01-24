using RestSharp;
using TheContentor.Infrastructure.Scrappers.Reddit;
using TheContentor.Infrastructure.Scrappers.Reddit.Models;

var client = new RestClient();

var scrapper = new RedditScrapper(client);

var result = await scrapper.ScrapeListAsync(new RedditScrapperRequest()
{
    Limit = 5,
    RedditSort = RedditSort.Top,
    Subreddit = "python"
});

var post = await scrapper.ScrapeItemAsync(result.First());

Console.ReadLine();