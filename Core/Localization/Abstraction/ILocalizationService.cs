namespace Core.Localization.Abstraction;

public interface ILocalizationService
{
    Task<string> GetLocalizedAsync(string key, string? keySection = null);

    Task<string> GetLocalizedAsync(
        string key,
        ICollection<string> acceptLocales,
        string? keySection = null);
}