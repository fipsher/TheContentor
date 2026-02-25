using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Application.Features.TtsPreview.Models;

namespace TheContentor.Application.Features.TtsPreview.Commands;

/// <summary>Generates a one-off TTS audio preview for the given text and settings.</summary>
public record GenerateTtsPreviewCommand(string Text, TtsSettingsModel Settings) : IRequest<TtsPreviewResultModel>;

/// <summary>Calls the tts-preview HTTP server and returns the generated audio path.</summary>
public class GenerateTtsPreviewCommandHandler(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IRequestHandler<GenerateTtsPreviewCommand, TtsPreviewResultModel>
{
    /// <summary>Sends the preview request to the Python TTS server.</summary>
    public async Task<TtsPreviewResultModel> Handle(GenerateTtsPreviewCommand request, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["TtsPreview:Url"] ?? "http://localhost:8765";
        var client = httpClientFactory.CreateClient();

        // Use StringContent so Content-Length is set on the request.
        // JsonContent (PostAsJsonAsync) returns false from TryComputeLength, causing HttpClient
        // to use Transfer-Encoding: chunked, which Python's BaseHTTPRequestHandler cannot decode.
        var json = JsonSerializer.Serialize(new
        {
            text = request.Text,
            voice = request.Settings.Voice,
            rate = request.Settings.Rate,
            pitch = request.Settings.Pitch,
            engine = request.Settings.Engine.ToString()
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{baseUrl}/preview", content, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TtsPreviewResultModel>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response from TTS preview server");
    }
}