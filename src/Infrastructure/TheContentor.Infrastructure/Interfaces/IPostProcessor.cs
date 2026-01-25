using TheContentor.Domain.Enums;
using TheContentor.Infrastructure.Models;

namespace TheContentor.Infrastructure.Interfaces;

public interface IPostProcessor
{
    Task<ProcessedPostResponse> ProcessAsync(string title, string content, CriteriaEngine engine = CriteriaEngine.Gemini, CancellationToken cancellationToken = default);
}
