using System.Net.Http.Json;
using System.Text.Json;
using Application.Services.StoryGeneration;
using Core.CrossCuttingConcerns.Exception.Types;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters.Anthropic;

/// Low-level Anthropic Messages API client (https://api.anthropic.com/v1/messages).
public class AnthropicClient
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;

    public AnthropicClient(HttpClient http, IOptions<AnthropicOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    /// Sends a single-turn message and returns the assistant's text.
    public async Task<string> CompleteAsync(
        string model,
        string system,
        string userContent,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new BusinessException("Anthropic API anahtarı yapılandırılmamış.");

        var payload = new
        {
            model,
            max_tokens = maxTokens,
            system,
            messages = new[] { new { role = "user", content = userContent } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        string raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new BusinessException($"Masal üretilemedi (Anthropic {(int)response.StatusCode}).");

        using JsonDocument doc = JsonDocument.Parse(raw);
        // { "content": [ { "type": "text", "text": "..." } ], ... }
        if (doc.RootElement.TryGetProperty("content", out JsonElement content) &&
            content.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out JsonElement type) && type.GetString() == "text" &&
                    block.TryGetProperty("text", out JsonElement text))
                {
                    return text.GetString() ?? "";
                }
            }
        }
        throw new BusinessException("Anthropic yanıtı beklenmeyen biçimde.");
    }
}
