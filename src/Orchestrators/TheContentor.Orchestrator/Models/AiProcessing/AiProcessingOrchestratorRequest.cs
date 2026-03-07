namespace TheContentor.Orchestrator.Models.AiProcessing;

/// <summary>Request to trigger the AI processing orchestration for a source post.</summary>
public class AiProcessingOrchestratorRequest
{
    /// <summary>Source post identifier.</summary>
    public Guid SourcePostId { get; set; }
    /// <summary>Optional override for the number of parts.</summary>
    public int? PartsCount { get; set; }
    /// <summary>Optional override for words per part.</summary>
    public int? WordsPerPart { get; set; }
    /// <summary>LLM provider name (e.g. "Gemini", "OpenAI").</summary>
    public string LlmProvider { get; set; } = "Gemini";
}
