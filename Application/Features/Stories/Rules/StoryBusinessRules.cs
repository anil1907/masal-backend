using Application.Features.Stories.Constants;
using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exception.Types;
using Core.Localization.Abstraction;

namespace Application.Features.Stories.Rules;

public class StoryBusinessRules : BaseBusinessRules
{
    private readonly ILocalizationService _localizationService;

    public StoryBusinessRules(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public async Task DailyGenerationLimitShouldNotBeExceeded(int generatedInLastDay, int dailyLimit)
    {
        if (generatedInLastDay >= dailyLimit)
            await throwBusinessException(StoryBusinessMessages.DailyGenerationLimitExceeded);
    }

    /// A chapter must never reach a child unless the safety gate passed.
    public async Task StoryShouldPassSafety(bool safetyPassed)
    {
        if (!safetyPassed)
            await throwBusinessException(StoryBusinessMessages.StoryFailedSafetyCheck);
    }

    public async Task ChapterShouldExist(object? chapter)
    {
        if (chapter is null)
            await throwBusinessException(StoryBusinessMessages.ChapterNotFound);
    }

    private async Task throwBusinessException(string messageKey)
    {
        string message = await _localizationService.GetLocalizedAsync(messageKey, StoryBusinessMessages.SectionName);
        throw new BusinessException(message);
    }
}
