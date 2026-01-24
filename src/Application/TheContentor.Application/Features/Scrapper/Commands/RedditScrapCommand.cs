using AutoMapper;
using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Scrappers.Reddit.Models;
using TheContentor.Infrastructure.Scrappers.Shared;

namespace TheContentor.Application.Features.Scrapper.Commands;

public record RedditScrapCommand : IRequest
{
    public required string Subreddit { get; set; }
    public required RedditSort RedditSort { get; set; }
    
    public int? Limit { get; set; }
    public int? Depth { get; set; }
    public string? After { get; set; }
}

public class ScrapCommandHandler(TheContentorDbContext context, ISourceScraper<RedditPost, RedditScrapperRequest> scrapper, IMapper mapper) : IRequestHandler<RedditScrapCommand>
{
    public async Task Handle(RedditScrapCommand request, CancellationToken cancellationToken)
    {
        var result = (await scrapper.ScrapeListAsync(new RedditScrapperRequest()
        {
            Limit = request.Limit,
            Depth = request.Depth,
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

        foreach (var item in mappedItems)
        {
            var postId = Guid.NewGuid();
            item.PostData.Id = postId;
            item.SourcePost.Id = postId;
            context.Add(item.PostData);
            context.Add(item.SourcePost);
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }
}