using System.Text.Json;
using Application.Services.StoryGeneration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters.Anthropic;

/// Post-hoc child-safety check on the generated story text. Separate model call (not just a
/// generation-prompt instruction). Fails CLOSED: any error/ambiguity -> not passed.
public class AnthropicSafetyGate : IStorySafetyGate
{
    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;

    public AnthropicSafetyGate(AnthropicClient client, IOptions<AnthropicOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<SafetyVerdict> EvaluateAsync(
        string storyText,
        IReadOnlyList<string> childFears,
        string? ageBand,
        CancellationToken cancellationToken = default)
    {
        string fears = childFears.Count == 0 ? "belirtilmedi" : string.Join(", ", childFears);
        string age = string.IsNullOrWhiteSpace(ageBand) ? "3-8" : ageBand!;

        string system = """
        Sen bir çocuk içeriği güvenlik denetçisisin. Sana verilen Türkçe uyku masalının küçük çocuklar için
        güvenli ve uygun olup olmadığını değerlendir. ŞUNLARDAN herhangi biri varsa REDDET:
        - şiddet, yaralanma, ölüm, gerçek tehlike, korku/dehşet, kötü/üzücü son
        - cinsellik, masum dostluk dışında romantizm, küfür, madde
        - gerçek dünyada tehlikeli bir davranışın olumlu gösterilmesi
        - çocuğun belirtilen korkularının bir tehdit/korkutma aracı olarak kullanılması
        Sadece şu JSON'u döndür: {"passed": true|false, "reason": "kısa Türkçe gerekçe"}
        """;

        string user = $"""
        Çocuğun yaşı: {age}
        Çocuğun korkuları (tehdit olarak KULLANILMAMALI): {fears}

        MASAL:
        {storyText}
        """;

        try
        {
            string raw = await _client.CompleteAsync(
                model: _options.SafetyModel,
                system: system,
                userContent: user,
                maxTokens: 300,
                cancellationToken: cancellationToken);

            return Parse(raw);
        }
        catch
        {
            // Fail closed.
            return new SafetyVerdict(false, "Güvenlik denetimi tamamlanamadı.");
        }
    }

    private static SafetyVerdict Parse(string raw)
    {
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        string json = (start >= 0 && end > start) ? raw[start..(end + 1)] : raw;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            bool passed = root.TryGetProperty("passed", out var p) && p.ValueKind == JsonValueKind.True;
            string reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
            return new SafetyVerdict(passed, reason);
        }
        catch (JsonException)
        {
            return new SafetyVerdict(false, "Güvenlik yanıtı çözümlenemedi.");
        }
    }
}
