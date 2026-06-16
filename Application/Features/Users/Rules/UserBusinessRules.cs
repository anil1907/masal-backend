using Application.Features.Users.Constants;
using Application.Services.Repositories;
using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exception.Types;
using Core.Localization.Abstraction;
using Domain.Entities.Users;

namespace Application.Features.Users.Rules;

public class UserBusinessRules : BaseBusinessRules
{
    private readonly IUserRepository _userRepository;
    private readonly ILocalizationService _localizationService;

    public UserBusinessRules(IUserRepository userRepository, ILocalizationService localizationService)
    {
        _userRepository = userRepository;
        _localizationService = localizationService;
    }

    public async Task UserShouldExistWhenSelected(User? user)
    {
        if (user == null)
            await throwBusinessException(UserBusinessMessages.UserNotExists);
    }

    public async Task UserShouldNotExistWhenCreating(string username, string email)
    {
        User? user = await _userRepository.GetAsync(u => u.Username == username || u.Email == email);
        if (user != null)
        {
            if (user.Username == username)
                await throwBusinessException(UserBusinessMessages.UsernameAlreadyExists);
            if (user.Email == email)
                await throwBusinessException(UserBusinessMessages.EmailAlreadyExists);
        }
    }

    public async Task UserShouldNotExistWhenUpdating(long id, string username, string email)
    {
        User? user = await _userRepository.GetAsync(u => (u.Username == username || u.Email == email) && u.Id != id);
        if (user != null)
        {
            if (user.Username == username)
                await throwBusinessException(UserBusinessMessages.UsernameAlreadyExists);
            if (user.Email == email)
                await throwBusinessException(UserBusinessMessages.EmailAlreadyExists);
        }
    }

    private async Task throwBusinessException(string messageKey)
    {
        string message = await _localizationService.GetLocalizedAsync(messageKey, UserBusinessMessages.SectionName);
        throw new BusinessException(message);
    }
}
