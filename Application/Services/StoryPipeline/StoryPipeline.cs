using Application.Features.Stories.Rules;
using Application.Persistence;
using Application.Services.AudioStorage;
using Application.Services.Push;
using Application.Services.StoryGeneration;
using Application.Services.Tts;
using Core.CrossCuttingConcerns.Exception.Types;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Services.StoryPipeline;

public class StoryPipeline : IStoryPipeline
{
    // Google TTS returns 64 kbps CBR MP3; bytes*8/bitrate is a good length estimate.
    private const int TtsBitrate = 64_000;

    private readonly IApplicationDbContext _db;
    private readonly IStoryGenerator _generator;
    private readonly IStorySafetyGate _safetyGate;
    private readonly ITtsSynthesizer _tts;
    private readonly IAudioStorage _audio;
    private readonly StoryBusinessRules _storyBusinessRules;
    private readonly StorySettings _storySettings;
    private readonly IPushSender _push;

    public StoryPipeline(
        IApplicationDbContext db,
        IStoryGenerator generator,
        IStorySafetyGate safetyGate,
        ITtsSynthesizer tts,
        IAudioStorage audio,
        StoryBusinessRules storyBusinessRules,
        IOptions<StorySettings> storySettings,
        IPushSender push)
    {
        _db = db;
        _generator = generator;
        _safetyGate = safetyGate;
        _tts = tts;
        _audio = audio;
        _storyBusinessRules = storyBusinessRules;
        _storySettings = storySettings.Value;
        _push = push;
    }

    public async Task GenerateNextChapterAsync(long userId, long childId, CancellationToken cancellationToken = default)
    {
        // Resolve the EXACT child the job was enqueued for (the user may have switched active
        // child between enqueue and processing) - fall back to active for legacy jobs.
        Child? child = childId > 0
            ? await _db.Children.FirstOrDefaultAsync(c => c.Id == childId && c.UserId == userId, cancellationToken)
            : await _db.Children
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        if (child is null)
            throw new BusinessException("Çocuk profili bulunamadı.");

        // Continue the child's ACTIVE series (create one if somehow missing).
        StorySeries? series = await _db.StorySeries
            .AsNoTracking()
            .Where(s => s.ChildId == child.Id && s.IsActive)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (series is null)
        {
            series = new StorySeries { ChildId = child.Id, Title = "Yeni masal", IsActive = true };
            _db.StorySeries.Add(series);
            await _db.SaveChangesAsync(cancellationToken);
        }

        StoryChapter? latest = await _db.StoryChapters
            .AsNoTracking()
            .Where(c => c.SeriesId == series.Id)
            .OrderByDescending(c => c.Number)
            .FirstOrDefaultAsync(cancellationToken);
        int number = latest is null ? 1 : latest.Number + 1;
        string? previousSummary = latest?.Summary;

        // Backstop cost guard (the daily/listened + subscription gating already limits this).
        int generatedToday = await _db.StoryGenerationLogs
            .AsNoTracking()
            .CountAsync(l => l.UserId == userId && l.CreatedDate >= DateTime.UtcNow.AddDays(-1), cancellationToken);
        await _storyBusinessRules.DailyGenerationLimitShouldNotBeExceeded(
            generatedToday, _storySettings.DailyGenerationLimit);

        var input = new StoryGenerationInput(
            HeroName: child.HeroName,
            Fears: child.Fears,
            Interests: child.Interests,
            AgeBand: child.AgeBand,
            ChapterNumber: number,
            PreviousSummary: previousSummary,
            Gender: child.Gender);

        GeneratedChapter generated = await _generator.GenerateAsync(input, cancellationToken);
        SafetyVerdict verdict = await _safetyGate.EvaluateAsync(generated.Text, child.Fears, child.AgeBand, cancellationToken);

        // The LLM cost was incurred regardless of the verdict - count it now.
        _db.StoryGenerationLogs.Add(new StoryGenerationLog { UserId = userId });
        await _db.SaveChangesAsync(cancellationToken);

        // Mandatory safety gate: never synthesize/store a chapter that didn't pass.
        await _storyBusinessRules.StoryShouldPassSafety(verdict.Passed);

        byte[] mp3 = await _tts.SynthesizeMp3Async(generated.Text, cancellationToken);
        string objectKey = $"chapters/{child.Id}/{series.Id}-{number}-{Guid.NewGuid():N}.mp3";
        await _audio.UploadMp3Async(mp3, objectKey, cancellationToken);

        var chapter = new StoryChapter
        {
            ChildId = child.Id,
            SeriesId = series.Id,
            Number = number,
            Title = generated.Title,
            Text = generated.Text,
            Summary = generated.Summary,
            AudioObjectKey = objectKey,
            DurationSeconds = (int)(mp3.Length * 8L / TtsBitrate)
        };
        _db.StoryChapters.Add(chapter);
        await _db.SaveChangesAsync(cancellationToken);

        // First chapter of a series names the series (from the generated title).
        if (number == 1)
        {
            StorySeries? tracked = await _db.StorySeries
                .FirstOrDefaultAsync(s => s.Id == series.Id && s.ChildId == child.Id, cancellationToken);
            if (tracked is not null)
            {
                tracked.Title = DeriveSeriesTitle(generated.Title);
                _db.StorySeries.Update(tracked);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        // Notify the parent that the bedtime story is ready. Best-effort: a push failure
        // must never fail the generation that already succeeded and was persisted.
        try
        {
            await _push.SendToUserAsync(
                userId,
                "Masal hazır 🌙",
                $"{child.HeroName} için yeni masal seni bekliyor.",
                cancellationToken);
        }
        catch
        {
            // swallowed by design
        }
    }

    /// "Fındık'ın Uzay Maceraları - Bölüm 1: ..." -> "Fındık'ın Uzay Maceraları".
    private static string DeriveSeriesTitle(string chapterTitle)
    {
        int sep = chapterTitle.IndexOf(" - ", StringComparison.Ordinal);
        string title = sep > 0 ? chapterTitle[..sep] : chapterTitle;
        return string.IsNullOrWhiteSpace(title) ? "Masal serisi" : title.Trim();
    }
}
