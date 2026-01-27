namespace TheContentor.Infrastructure.Options;

/// <summary>
/// Options for LLM providers.
/// </summary>
public class LlmOptions
{
    /// <summary>
    /// Section name in configuration.
    /// </summary>
    public const string SectionName = "LLM";

    /// <summary>
    /// Options for Gemini.
    /// </summary>
    public GeminiOptions Gemini { get; set; } = new();

    /// <summary>
    /// Options for ChatGPT.
    /// </summary>
    public ChatGPTOptions ChatGPT { get; set; } = new();
}

/// <summary>
/// Options for Gemini provider.
/// </summary>
public class GeminiOptions
{
    /// <summary>
    /// API Key for Gemini.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Options for ChatGPT provider.
/// </summary>
public class ChatGPTOptions
{
    /// <summary>
    /// API Key for ChatGPT.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
