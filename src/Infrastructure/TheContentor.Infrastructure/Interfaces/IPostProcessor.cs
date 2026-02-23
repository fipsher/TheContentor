using TheContentor.Domain.Enums;
using TheContentor.Infrastructure.Models;

namespace TheContentor.Infrastructure.Interfaces;

public interface IPostProcessor
{
    /// <summary>Processes a source post using the specified LLM provider.</summary>
    Task<ProcessedPostResponse> ProcessAsync(
        string title,
        string content,
        int? partsCount = null,
        int? wordsPerPart = null,
        LlmProvider provider = LlmProvider.Gemini,
        CancellationToken cancellationToken = default);
}
