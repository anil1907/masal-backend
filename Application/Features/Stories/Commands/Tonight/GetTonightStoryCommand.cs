using Application.Features.Children.Rules;
using Application.Features.Stories.Dtos;
using Application.Features.Stories.Rules;
using Application.Features.Subscriptions.Constants;
using Application.Services.AudioStorage;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Application.Services.StoryGeneration;
using Application.Services.Tts;
using Core.Application.Pipelines.Authorization;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Stories.Commands.Tonight;

/// The home-screen story state. Persisted + cost-aware:
/// - no chapter yet            -> generate chapter 1 (first story).
/// - latest not yet listened   -> return it (NO generation, just a fresh signed URL).
/// - latest listened earlier   -> a new day: generate the next chapter from the running summary.
/// - latest listened today     -> "come back tomorrow" (1 story/day).
/// Generation (LLM x2 + TTS) therefore happens at most once per day, only after the previous
/// chapter was actually heard. POST because the "generate" branch creates state; safe (idempotent
/// within a day) on the other branches.
public class GetTonightStoryCommand : IRequest<TonightStoryResponse>, ISecuredRequest
{
    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GetTonightStoryCommandHandler : IRequestHandler<GetTonightStoryCommand, TonightStoryResponse>
    {
        // Google TTS returns 64 kbps CBR MP3; bytes*8/bitrate is a good length estimate.
        private const int TtsBitrate = 64_000;

        private readonly IChildRepository _childRepository;
        private readonly IStoryChapterRepository _chapterRepository;
        private readonly IStoryGenerationLogRepository _generationLogRepository;
        private readonly IEntitlementRepository _entitlementRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IStoryGenerator _generator;
        private readonly IStorySafetyGate _safetyGate;
        private readonly ITtsSynthesizer _tts;
        private readonly IAudioStorage _audio;
        private readonly ChildBusinessRules _childBusinessRules;
        private readonly StoryBusinessRules _storyBusinessRules;
        private readonly StorySettings _storySettings;

        public GetTonightStoryCommandHandler(
            IChildRepository childRepository,
            IStoryChapterRepository chapterRepository,
            IStoryGenerationLogRepository generationLogRepository,
            IEntitlementRepository entitlementRepository,
            ICurrentUser currentUser,
            IStoryGenerator generator,
            IStorySafetyGate safetyGate,
            ITtsSynthesizer tts,
            IAudioStorage audio,
            ChildBusinessRules childBusinessRules,
            StoryBusinessRules storyBusinessRules,
            IOptions<StorySettings> storySettings)
        {
            _childRepository = childRepository;
            _chapterRepository = chapterRepository;
            _generationLogRepository = generationLogRepository;
            _entitlementRepository = entitlementRepository;
            _currentUser = currentUser;
            _generator = generator;
            _safetyGate = safetyGate;
            _tts = tts;
            _audio = audio;
            _childBusinessRules = childBusinessRules;
            _storyBusinessRules = storyBusinessRules;
            _storySettings = storySettings.Value;
        }

        public async Task<TonightStoryResponse> Handle(GetTonightStoryCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            StoryChapter? latest = await _chapterRepository.GetLatestForChildAsync(child!.Id, cancellationToken);

            // Subscription gate: premium gets the daily arc; free is capped per rolling week.
            bool premium = await _entitlementRepository.GetActiveByUserIdAsync(userId, DateTime.UtcNow, cancellationToken) is not null;
            bool canGenerateNew = premium || await IsUnderFreeWeeklyLimitAsync(child.Id, cancellationToken);

            // First story ever.
            if (latest is null)
            {
                if (!canGenerateNew)
                    return await FreeLimitReachedAsync(null, cancellationToken);
                StoryChapter first = await GenerateAndStoreAsync(userId, child, number: 1, previousSummary: null, cancellationToken);
                return await ReadyAsync(first, cancellationToken);
            }

            // Current chapter not finished yet - keep serving it, no new cost (within allowance).
            if (latest.ListenedDate is null)
                return await ReadyAsync(latest, cancellationToken);

            // Listened. One story per day: the next one unlocks the day after it was heard.
            DateTime today = DateTime.UtcNow.Date;
            if (latest.ListenedDate.Value.Date >= today)
                return await ComeBackTomorrowAsync(latest, cancellationToken);

            // New day and the previous chapter was heard. Free tier must still have weekly budget.
            if (!canGenerateNew)
                return await FreeLimitReachedAsync(latest, cancellationToken);

            StoryChapter next = await GenerateAndStoreAsync(
                userId, child, number: latest.Number + 1, previousSummary: latest.Summary, cancellationToken);
            return await ReadyAsync(next, cancellationToken);
        }

        private async Task<bool> IsUnderFreeWeeklyLimitAsync(long childId, CancellationToken ct)
        {
            int thisWeek = await _chapterRepository.CountForChildSinceAsync(
                childId, DateTime.UtcNow.AddDays(-7), ct);
            return thisWeek < SubscriptionConstants.WeeklyFreeStories;
        }

        private async Task<StoryChapter> GenerateAndStoreAsync(
            long userId, Child child, int number, string? previousSummary, CancellationToken cancellationToken)
        {
            // Backstop cost guard (the daily/listened gating already limits this to ~1/day).
            int generatedToday = await _generationLogRepository.CountForUserSinceAsync(
                userId, DateTime.UtcNow.AddDays(-1), cancellationToken);
            await _storyBusinessRules.DailyGenerationLimitShouldNotBeExceeded(
                generatedToday, _storySettings.DailyGenerationLimit);

            var input = new StoryGenerationInput(
                HeroName: child.HeroName,
                Fears: child.Fears,
                Interests: child.Interests,
                AgeBand: child.AgeBand,
                ChapterNumber: number,
                PreviousSummary: previousSummary);

            GeneratedChapter generated = await _generator.GenerateAsync(input, cancellationToken);
            SafetyVerdict verdict = await _safetyGate.EvaluateAsync(generated.Text, child.Fears, child.AgeBand, cancellationToken);

            // The LLM cost was incurred regardless of the verdict - count it now.
            await _generationLogRepository.AddAsync(new StoryGenerationLog { UserId = userId }, cancellationToken);

            // Mandatory safety gate: never synthesize/store a chapter that didn't pass.
            await _storyBusinessRules.StoryShouldPassSafety(verdict.Passed);

            byte[] mp3 = await _tts.SynthesizeMp3Async(generated.Text, cancellationToken);
            string objectKey = $"chapters/{child.Id}/{number}-{Guid.NewGuid():N}.mp3";
            await _audio.UploadMp3Async(mp3, objectKey, cancellationToken);

            var chapter = new StoryChapter
            {
                ChildId = child.Id,
                Number = number,
                Title = generated.Title,
                Text = generated.Text,
                Summary = generated.Summary,
                AudioObjectKey = objectKey,
                DurationSeconds = (int)(mp3.Length * 8L / TtsBitrate)
            };
            return await _chapterRepository.AddAsync(chapter, cancellationToken);
        }

        private async Task<TonightStoryResponse> ReadyAsync(StoryChapter chapter, CancellationToken ct)
            => new()
            {
                Status = TonightStoryResponse.StatusReady,
                Available = true,
                Chapter = await ChapterDto.FromAsync(chapter, _audio, ct)
            };

        private async Task<TonightStoryResponse> ComeBackTomorrowAsync(StoryChapter last, CancellationToken ct)
            => new()
            {
                Status = TonightStoryResponse.StatusComeBackTomorrow,
                Available = false,
                Chapter = await ChapterDto.FromAsync(last, _audio, ct)
            };

        private async Task<TonightStoryResponse> FreeLimitReachedAsync(StoryChapter? last, CancellationToken ct)
            => new()
            {
                Status = TonightStoryResponse.StatusFreeLimitReached,
                Available = false,
                Chapter = last is null ? null : await ChapterDto.FromAsync(last, _audio, ct)
            };
    }
}
