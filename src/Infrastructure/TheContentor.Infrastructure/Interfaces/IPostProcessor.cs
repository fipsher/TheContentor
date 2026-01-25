using TheContentor.Infrastructure.Models;

namespace TheContentor.Infrastructure.Interfaces;

public interface IPostProcessor
{
    Task<ProcessedPostResponse> ProcessAsync(string title, string content, CancellationToken cancellationToken = default);
}
