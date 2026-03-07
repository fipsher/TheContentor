namespace TheContentor.Domain.Enums;

/// <summary>LLM provider used for source post AI processing.</summary>
public enum LlmProvider
{
    /// <summary>Google Gemini.</summary>
    Gemini = 0,
    /// <summary>OpenAI ChatGPT.</summary>
    OpenAI = 1,
    /// <summary>Local LLM via Ollama OpenAI-compatible endpoint.</summary>
    Local = 2
}