using System.Net.Http.Json;
using System.Text.Json;
using Application.Services.Tts;
using Core.CrossCuttingConcerns.Exception.Types;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters.GoogleTts;

/// Google Cloud Text-to-Speech via the REST API + API key:
/// POST https://texttospeech.googleapis.com/v1/text:synthesize?key=API_KEY
/// Returns base64 MP3 in `audioContent`.
public class GoogleTtsSynthesizer : ITtsSynthesizer
{
    private readonly HttpClient _http;
    private readonly GoogleTtsOptions _options;

    public GoogleTtsSynthesizer(HttpClient http, IOptions<GoogleTtsOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<byte[]> SynthesizeMp3Async(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new BusinessException("Google TTS API anahtarı yapılandırılmamış.");
        if (string.IsNullOrWhiteSpace(text))
            throw new BusinessException("Seslendirilecek metin boş.");

        var payload = new
        {
            input = new { text },
            voice = new { languageCode = _options.LanguageCode, name = _options.VoiceName },
            audioConfig = new
            {
                audioEncoding = "MP3",
                speakingRate = _options.SpeakingRate,
                pitch = _options.Pitch
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/text:synthesize?key={_options.ApiKey}")
        {
            Content = JsonContent.Create(payload)
        };

        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        string raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new BusinessException($"Seslendirme başarısız (Google TTS {(int)response.StatusCode}).");

        using JsonDocument doc = JsonDocument.Parse(raw);
        if (doc.RootElement.TryGetProperty("audioContent", out JsonElement audio) &&
            audio.GetString() is string base64 && base64.Length > 0)
        {
            return Convert.FromBase64String(base64);
        }
        throw new BusinessException("Google TTS yanıtı beklenmeyen biçimde.");
    }
}
