using TheContentor.Infrastructure.Interfaces;
using TheContentor.Infrastructure.Models;

namespace TheContentor.Infrastructure.Services;

public class PostProcessor : IPostProcessor
{
    public Task<ProcessedPostResponse> ProcessAsync(string title, string content, CancellationToken cancellationToken = default)
    {
        // For now, it's a mock that "processes" the text
        var response = new ProcessedPostResponse
        {
            Title = $"Social: {title}",
            Description = $"Check out this post: {content.Substring(0, Math.Min(content.Length, 100))}...",
            Hashtags = new List<string> { "AI", "Content", "Automation" },
            Parts = new List<ProcessedPostPartResponse>
            {
                new()
                {
                    Part = 1,
                    ProcessedText = $"[Processed Title]: {title}\n\n[Processed Content]: {content}",
                    Hashtags = new List<string> { "SocialMedia" }
                },
                new()
                {
                    Part = 2,
                    ProcessedText = $"[Processed Title]: {title}\n\n[Processed Content]: {content}",
                    Hashtags = new List<string> { "SocialMedia" }
                }
            }
        };

        return Task.FromResult(response);
    }
}
