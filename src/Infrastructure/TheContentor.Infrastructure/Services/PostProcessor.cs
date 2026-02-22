using System.Text.Json;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TheContentor.Infrastructure.Constants;
using TheContentor.Infrastructure.Interfaces;
using TheContentor.Infrastructure.Models;
using TheContentor.Infrastructure.Options;

namespace TheContentor.Infrastructure.Services;

/// <summary>
/// Service for processing post content using LLMs (Gemini or ChatGPT).
/// </summary>
public class PostProcessor(
    IOptions<LlmOptions> llmOptions,
    ILogger<PostProcessor> logger) : IPostProcessor
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly LlmOptions _options = llmOptions.Value;

    /// <inheritdoc />
    public async Task<ProcessedPostResponse> ProcessAsync(
        string title,
        string content,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing post. Title: {Title}", title);

        try
        {
            return await ProcessWithGeminiAsync(title, content, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing post");
            throw;
        }
    }

    private async Task<ProcessedPostResponse> ProcessWithGeminiAsync(
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        var apiKey = _options.Gemini.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Gemini API Key is not configured in LLM:Gemini:ApiKey");
        }

        await using var client = new Google.GenAI.Client(apiKey: apiKey);
        var response = await client.Models.GenerateContentAsync(
            PromptConstants.GeminiModel,
            [
                new Content
                {
                    Parts =
                    [
                        new Part { Text = PromptConstants.SystemPrompt },
                        new Part { Text = PromptConstants.GetUserPrompt(title, content) }
                    ],
                    Role = "user"
                },
            ],
            new GenerateContentConfig
            {
                ResponseMimeType = "application/json"
            });

        var text = response.Candidates?[0].Content?.Parts?[0].Text;

        if (string.IsNullOrEmpty(text))
        {
            throw new Exception("Gemini returned empty text content.");
        }

        return JsonSerializer.Deserialize<ProcessedPostResponse>(text, _jsonOptions)
               ?? throw new Exception("Failed to deserialize Gemini response to ProcessedPostResponse.");
    }

    private async Task<ProcessedPostResponse> ProcessWithChatGPTAsync(
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        var apiKey = _options.ChatGPT.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("ChatGPT API Key is not configured in LLM:ChatGPT:ApiKey");
        }

        ChatClient client = new(PromptConstants.ChatGPTModel, apiKey);
        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        ChatCompletion completion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(PromptConstants.SystemPrompt),
                new UserChatMessage(PromptConstants.GetUserPrompt(title, content))
            ],
            options,
            cancellationToken);

        var text = completion.Content[0].Text;

        if (string.IsNullOrEmpty(text))
        {
            throw new Exception("ChatGPT returned empty text content.");
        }

        return JsonSerializer.Deserialize<ProcessedPostResponse>(text, _jsonOptions)
               ?? throw new Exception("Failed to deserialize ChatGPT response to ProcessedPostResponse.");
    }
}