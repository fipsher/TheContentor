using AutoMapper;
using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Scrappers.Reddit.Models;
using TheContentor.Infrastructure.Scrappers.Shared;

namespace TheContentor.Application.Features.Scrapper.Commands;

/// <summary>Scrapes a subreddit listing into the local store.</summary>
public record RedditScrapCommand : IRequest
{
    /// <summary>Target subreddit name to scrape.</summary>
    public required string Subreddit { get; set; }
    /// <summary>Sorting strategy used for the Reddit listing.</summary>
    public required RedditSort RedditSort { get; set; }

    /// <summary>Optional maximum number of posts to fetch.</summary>
    public int? Limit { get; set; }
    /// <summary>Optional pagination cursor for the next page.</summary>
    public string? After { get; set; }
}

/// <summary>Fetches Reddit posts and persists new items.</summary>
public class ScrapCommandHandler(TheContentorDbContext context, ISourceScraper<RedditPost, RedditScrapperRequest> scrapper, IMapper mapper) : IRequestHandler<RedditScrapCommand>
{
    /// <summary>Runs the scrape and saves new posts.</summary>
    public async Task Handle(RedditScrapCommand request, CancellationToken cancellationToken)
    {
        var result = (await scrapper.ScrapeListAsync(new RedditScrapperRequest()
        {
            Limit = request.Limit,
            After = request.After,
            RedditSort = request.RedditSort,
            Subreddit = request.Subreddit
        })).ToList();
        
        if (result.Count == 0) { return; }

        var mappedItems = result.Select(post => new
        {
            PostData = mapper.Map<RedditPostData>(post),
            SourcePost = mapper.Map<SourcePost>(post),
        }).ToList();

        var externalIds = mappedItems.Select(i => i.SourcePost.ExternalId).ToList();
        var existingExternalIds = context.SourcePosts
            .Where(p => externalIds.Contains(p.ExternalId))
            .Select(p => p.ExternalId)
            .ToHashSet();

        foreach (var item in mappedItems)
        {
            if (existingExternalIds.Contains(item.SourcePost.ExternalId))
            {
                continue;
            }
            
            var postId = Guid.NewGuid();
            item.PostData.Id = postId;
            item.SourcePost.Id = postId;
            context.Add(item.PostData);
            context.Add(item.SourcePost);
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }
}
