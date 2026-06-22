using System.Text;
using System.Text.Json;
using Application.Services.StoryGeneration;
using Core.CrossCuttingConcerns.Exception.Types;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters.Anthropic;

public class AnthropicStoryGenerator : IStoryGenerator
{
    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;

    public AnthropicStoryGenerator(AnthropicClient client, IOptions<AnthropicOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<GeneratedChapter> GenerateAsync(StoryGenerationInput input, CancellationToken cancellationToken = default)
    {
        string raw = await _client.CompleteAsync(
            model: _options.Model,
            system: BuildSystemPrompt(input),
            userContent: BuildUserPrompt(input),
            maxTokens: 1500,
            cancellationToken: cancellationToken);

        return Parse(raw);
    }

    private static string BuildSystemPrompt(StoryGenerationInput input)
    {
        return """
        Sen çocuklar için uyku öncesi masallar yazan sıcak, şefkatli bir anlatıcısın.
        Kurallar:
        - Çocuğun yaş bandına göre dili, kelime dağarcığını ve uzunluğu uyarla (verilen yaşa uygun yaz).
        - Türkçe yaz. Basit, kısa cümleler; yumuşak ve sakinleştirici bir ton kullan.
        - Masal uykuya hazırlamalı: sonu huzurlu ve güven verici olmalı, heyecanla bitmemeli.
        - Kesinlikle yaşa uygun: şiddet, ölüm, gerçek tehlike, korku/dehşet, kötü son YOK.
        - Cinsellik, romantizm (masum dostluk dışında), küfür, madde YOK.
        - Çocuğun KORKULARINI asla bir tehdit ya da olay aracı olarak kullanma. Gerekirse nazikçe güven ver; en iyisi hiç değinmemek.
        - Kahramanın adını kullan ve ilgi alanlarını hikâyeye doğal şekilde dokur.
        - Uzunluk: tek bir uyku masalı (~300-450 kelime).
        - SADECE şu JSON'u döndür, başka hiçbir şey yazma:
          {"title": "kısa başlık", "story": "masalın tam metni", "summary": "1-2 cümlelik özet (yarın kaldığı yerden devam etmek için)"}
        """;
    }

    private static string BuildUserPrompt(StoryGenerationInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Kahramanın adı: {input.HeroName}");
        if (!string.IsNullOrWhiteSpace(input.Gender))
            sb.AppendLine($"Cinsiyet: {input.Gender} (uygun zamirleri/temsili buna göre kullan)");
        if (!string.IsNullOrWhiteSpace(input.AgeBand))
            sb.AppendLine($"Yaş: {input.AgeBand}");
        sb.AppendLine($"İlgi alanları: {Join(input.Interests, "yok")}");
        sb.AppendLine($"Kaçınılacak korkular: {Join(input.Fears, "belirtilmedi")}");
        sb.AppendLine($"Bölüm numarası: {input.ChapterNumber}");
        if (string.IsNullOrWhiteSpace(input.PreviousSummary))
            sb.AppendLine("Bu ilk bölüm. Dünyayı ve kahramanı tanıt, sakin bir başlangıç yap.");
        else
        {
            sb.AppendLine($"Önceki bölümün özeti: {input.PreviousSummary}");
            sb.AppendLine("Bu gece aynı hikâyeyi buradan devam ettir; yarın da sürebilecek huzurlu bir anla bitir.");
        }
        return sb.ToString();
    }

    private static string Join(IReadOnlyList<string> items, string fallback)
        => items.Count == 0 ? fallback : string.Join(", ", items);

    private static GeneratedChapter Parse(string raw)
    {
        string json = ExtractJson(raw);
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            string title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            string story = root.TryGetProperty("story", out var s) ? s.GetString() ?? "" : "";
            string summary = root.TryGetProperty("summary", out var su) ? su.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(story))
                throw new BusinessException("Masal metni boş döndü.");
            return new GeneratedChapter(title, story, summary);
        }
        catch (JsonException)
        {
            throw new BusinessException("Masal yanıtı çözümlenemedi.");
        }
    }

    /// Strips markdown fences / surrounding prose and returns the first {...} block.
    private static string ExtractJson(string raw)
    {
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        return (start >= 0 && end > start) ? raw[start..(end + 1)] : raw;
    }
}
