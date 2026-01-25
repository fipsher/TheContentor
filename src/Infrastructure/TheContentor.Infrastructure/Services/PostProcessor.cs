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
            Hashtags = new List<string> { "AI", "Content", "Automation" },
            Parts = new List<ProcessedPostPartResponse>
            {
                new()
                {
                    ProcessedText = $"[Processed Title]: {title}\n\n[Processed Content]: {content}",
                    Hashtags = new List<string> { "SocialMedia" }
                }
            }
        };

        return Task.FromResult(response);
    }
}
