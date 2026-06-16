using Application.Features.Children.Rules;
using Application.Features.Stories.Rules;
using Application.Services.CurrentUser;
using Application.Services.Repositories;
using Application.Services.StoryGeneration;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Logging;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Stories.Commands.GenerateChapter;

/// Dev/preview: generate tonight's chapter for the current user's child and run the safety gate.
/// (The persisted arc, library, nightly batch and TTS come next; this verifies the LLM end to end.)
public class GenerateChapterCommand : IRequest<GenerateChapterResponse>, ISecuredRequest, ILoggableRequest
{
    public int ChapterNumber { get; set; } = 1;
    public string? PreviousSummary { get; set; }

    public string[] Roles => [OperationClaims.AllowAnonymous];

    public class GenerateChapterCommandHandler : IRequestHandler<GenerateChapterCommand, GenerateChapterResponse>
    {
        private readonly IChildRepository _childRepository;
        private readonly IStoryGenerationLogRepository _generationLogRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IStoryGenerator _generator;
        private readonly IStorySafetyGate _safetyGate;
        private readonly ChildBusinessRules _childBusinessRules;
        private readonly StoryBusinessRules _storyBusinessRules;
        private readonly StorySettings _storySettings;

        public GenerateChapterCommandHandler(
            IChildRepository childRepository,
            IStoryGenerationLogRepository generationLogRepository,
            ICurrentUser currentUser,
            IStoryGenerator generator,
            IStorySafetyGate safetyGate,
            ChildBusinessRules childBusinessRules,
            StoryBusinessRules storyBusinessRules,
            IOptions<StorySettings> storySettings)
        {
            _childRepository = childRepository;
            _generationLogRepository = generationLogRepository;
            _currentUser = currentUser;
            _generator = generator;
            _safetyGate = safetyGate;
            _childBusinessRules = childBusinessRules;
            _storyBusinessRules = storyBusinessRules;
            _storySettings = storySettings.Value;
        }

        public async Task<GenerateChapterResponse> Handle(GenerateChapterCommand request, CancellationToken cancellationToken)
        {
            long userId = _currentUser.UserIdOrThrow();
            Child? child = await _childRepository.GetByUserIdAsync(userId, cancellationToken);
            await _childBusinessRules.ChildShouldExist(child);

            // Cost guard: every generation is two paid LLM calls - cap per user per rolling 24h.
            int generatedToday = await _generationLogRepository.CountForUserSinceAsync(
                userId, DateTime.UtcNow.AddDays(-1), cancellationToken);
            await _storyBusinessRules.DailyGenerationLimitShouldNotBeExceeded(
                generatedToday, _storySettings.DailyGenerationLimit);

            var input = new StoryGenerationInput(
                HeroName: child!.HeroName,
                Fears: child.Fears,
                Interests: child.Interests,
                AgeBand: child.AgeBand,
                ChapterNumber: request.ChapterNumber,
                PreviousSummary: request.PreviousSummary);

            GeneratedChapter chapter = await _generator.GenerateAsync(input, cancellationToken);
            SafetyVerdict verdict = await _safetyGate.EvaluateAsync(chapter.Text, child.Fears, child.AgeBand, cancellationToken);

            // Count against the quota regardless of the safety verdict - the cost was incurred.
            await _generationLogRepository.AddAsync(new StoryGenerationLog { UserId = userId }, cancellationToken);

            return new GenerateChapterResponse
            {
                Title = chapter.Title,
                Text = chapter.Text,
                Summary = chapter.Summary,
                SafetyPassed = verdict.Passed,
                SafetyReason = verdict.Reason
            };
        }
    }
}
