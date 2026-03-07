using System.Text.Json;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure.Constants;
using TheContentor.Infrastructure.Interfaces;
using TheContentor.Infrastructure.Models;
using TheContentor.Infrastructure.Options;

namespace TheContentor.Infrastructure.Services;

/// <summary>
/// Service for processing post content using LLMs (Gemini, ChatGPT, or local Ollama).
/// </summary>
public class PostProcessor(
    IOptions<LlmOptions> llmOptions,
    IChatClient ollamaChatClient,
    ILogger<PostProcessor> logger) : IPostProcessor
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly LlmOptions _options = llmOptions.Value;

    /// <inheritdoc />
    public async Task<ProcessedPostResponse> ProcessAsync(
        string title,
        string content,
        int? partsCount = null,
        int? wordsPerPart = null,
        LlmProvider provider = LlmProvider.Gemini,
        ProcessingMode mode = ProcessingMode.Classic,
        ProcessedPostResponse? existingProcessedPost = null,
        string? localModelName = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing post with {Provider}, mode {Mode}. Title: {Title}", provider, mode, title);

        try
        {
            var systemPrompt = PromptConstants.BuildSystemPrompt(partsCount, wordsPerPart);
            var userPrompt = PromptConstants.GetUserPrompt(title, content);

            return mode switch
            {
                ProcessingMode.FullPipeline => await ProcessFullPipelineAsync(title, content, systemPrompt, userPrompt, provider, localModelName, cancellationToken),
                ProcessingMode.EnhanceExisting => await ProcessEnhanceExistingAsync(title, content, existingProcessedPost!, provider, localModelName, cancellationToken),
                _ => await CallLlmAsync(systemPrompt, userPrompt, provider, localModelName, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing post with {Provider}", provider);
            throw;
        }
    }

    /// <summary>Dispatches to the appropriate LLM implementation.</summary>
    private Task<ProcessedPostResponse> CallLlmAsync(string systemPrompt, string userPrompt, LlmProvider provider, string? localModelName, CancellationToken cancellationToken) =>
        provider switch
        {
            LlmProvider.OpenAI => ProcessWithChatGPTAsync(systemPrompt, userPrompt, cancellationToken),
            LlmProvider.Local  => ProcessWithLocalAsync(systemPrompt, userPrompt, localModelName, cancellationToken),
            _                  => ProcessWithGeminiAsync(systemPrompt, userPrompt, cancellationToken)
        };

    /// <summary>Runs the three-step full pipeline: scriptwriter → creative refiner → retention critic.</summary>
    private async Task<ProcessedPostResponse> ProcessFullPipelineAsync(
        string title, string content, string step1SystemPrompt, string step1UserPrompt,
        LlmProvider provider, string? localModelName, CancellationToken cancellationToken)
    {
        logger.LogInformation("Full pipeline Step 1: Scripting...");
        var result1 = await CallLlmAsync(step1SystemPrompt, step1UserPrompt, provider, localModelName, cancellationToken);

        logger.LogInformation("Full pipeline Step 2: Creative refiner...");
        var step1Json = JsonSerializer.Serialize(result1, _jsonOptions);
        var result2 = await CallLlmAsync(
            PromptConstants.CreativeRefinerSystemPrompt,
            PromptConstants.GetCreativeRefinerUserPrompt(title, content, step1Json),
            provider, localModelName, cancellationToken);

        logger.LogInformation("Full pipeline Step 2.5: Retention critic...");
        var step2Json = JsonSerializer.Serialize(result2, _jsonOptions);
        var result3 = await CallLlmAsync(
            PromptConstants.RetentionCriticSystemPrompt,
            PromptConstants.GetRetentionCriticUserPrompt(step2Json),
            provider, localModelName, cancellationToken);

        return result3;
    }

    /// <summary>Runs refiner + critic on an already-processed post without rerunning the scriptwriter.</summary>
    private async Task<ProcessedPostResponse> ProcessEnhanceExistingAsync(
        string title, string content, ProcessedPostResponse existingProcessedPost,
        LlmProvider provider, string? localModelName, CancellationToken cancellationToken)
    {
        if (existingProcessedPost == null)
            throw new InvalidOperationException("EnhanceExisting mode requires an existing ProcessedPost.");

        logger.LogInformation("Enhance existing Step 2: Creative refiner...");
        var existingJson = JsonSerializer.Serialize(existingProcessedPost, _jsonOptions);
        var result2 = await CallLlmAsync(
            PromptConstants.CreativeRefinerSystemPrompt,
            PromptConstants.GetCreativeRefinerUserPrompt(title, content, existingJson),
            provider, localModelName, cancellationToken);

        logger.LogInformation("Enhance existing Step 2.5: Retention critic...");
        var step2Json = JsonSerializer.Serialize(result2, _jsonOptions);
        var result3 = await CallLlmAsync(
            PromptConstants.RetentionCriticSystemPrompt,
            PromptConstants.GetRetentionCriticUserPrompt(step2Json),
            provider, localModelName, cancellationToken);

        return result3;
    }

    private async Task<ProcessedPostResponse> ProcessWithGeminiAsync(
        string systemPrompt,
        string userPrompt,
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
                        new Part { Text = systemPrompt },
                        new Part { Text = userPrompt }
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
        string systemPrompt,
        string userPrompt,
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
            ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat()
        };

        ChatCompletion completion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
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

    /// <summary>Processes using a local Ollama model via the injected IChatClient.</summary>
    private async Task<ProcessedPostResponse> ProcessWithLocalAsync(
        string systemPrompt,
        string userPrompt,
        string? modelOverride,
        CancellationToken cancellationToken)
    {
        var model = !string.IsNullOrWhiteSpace(modelOverride) ? modelOverride : _options.Local.Model;

        var chatOptions = new ChatOptions
        {
            ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.Json,
            ModelId = string.IsNullOrEmpty(model) ? null : model
        };

        var response = await ollamaChatClient.GetResponseAsync(
            [
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, systemPrompt),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userPrompt)
            ],
            chatOptions,
            cancellationToken);

        var text = response.Text;
        if (string.IsNullOrEmpty(text))
            throw new Exception("Local LLM returned empty content.");

        return JsonSerializer.Deserialize<ProcessedPostResponse>(text, _jsonOptions)
               ?? throw new Exception("Failed to deserialize local LLM response.");
    }
}