using Application.Features.Children.Constants;
using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exception.Types;
using Core.Localization.Abstraction;
using Domain.Entities.Children;

namespace Application.Features.Children.Rules;

public class ChildBusinessRules : BaseBusinessRules
{
    private readonly ILocalizationService _localizationService;

    public ChildBusinessRules(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public async Task UserShouldNotAlreadyHaveChild(Child? existing)
    {
        if (existing != null)
            await throwBusinessException(ChildBusinessMessages.ChildAlreadyExists);
    }

    /// Enforce the per-account child cap: free = 1, premium = 3.
    public async Task UserCanAddChild(int currentCount, bool isPremium)
    {
        if (!isPremium && currentCount >= ChildConstants.MaxFreeChildren)
            await throwBusinessException(ChildBusinessMessages.FreeChildLimitReached);
        if (isPremium && currentCount >= ChildConstants.MaxPremiumChildren)
            await throwBusinessException(ChildBusinessMessages.PremiumChildLimitReached);
    }

    public async Task ChildShouldExist(Child? child)
    {
        if (child == null)
            await throwBusinessException(ChildBusinessMessages.ChildNotExists);
    }

    private async Task throwBusinessException(string messageKey)
    {
        string message = await _localizationService.GetLocalizedAsync(messageKey, ChildBusinessMessages.SectionName);
        throw new BusinessException(message);
    }
}
