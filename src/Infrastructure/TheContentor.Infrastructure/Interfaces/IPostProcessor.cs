using TheContentor.Domain.Enums;
using TheContentor.Infrastructure.Models;

namespace TheContentor.Infrastructure.Interfaces;

public interface IPostProcessor
{
    /// <summary>Processes a source post using the specified LLM provider and processing mode.</summary>
    Task<ProcessedPostResponse> ProcessAsync(
        string title,
        string content,
        int? partsCount = null,
        int? wordsPerPart = null,
        LlmProvider provider = LlmProvider.Gemini,
        ProcessingMode mode = ProcessingMode.Classic,
        ProcessedPostResponse? existingProcessedPost = null,
        CancellationToken cancellationToken = default);
}
