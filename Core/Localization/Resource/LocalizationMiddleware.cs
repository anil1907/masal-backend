using Core.Localization.Abstraction;
using Core.Localization.Resource.Yaml;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Core.Localization.Resource;

public class LocalizationMiddleware<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalizationService _localizationService;

    public LocalizationMiddleware(IHttpContextAccessor httpContextAccessor, ILocalizationService localizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _localizationService = localizationService;
    }


    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var acceptLanguage = _httpContextAccessor.HttpContext.Request.Headers["Accept-Language"].ToString(); // Örn: "tr-TR,tr;q=0.9,en;q=0.8"
        var locales = acceptLanguage
            .Split(',')
            .Select(lang => lang.Split(';')[0].Trim()) // "tr-TR;q=0.9" -> "tr-TR"
            .Where(lang => !string.IsNullOrWhiteSpace(lang))
            .Select(lang => NormalizeLocale(lang)) // Normalize locale codes (tr-TR -> tr, en-EN -> en, ru-RU -> ru)
            .ToList();

        if (_localizationService is ResourceLocalizationManager manager)
            manager.AcceptLocales = locales;

        return await next();
    }

    private static string NormalizeLocale(string locale)
    {
        // Extract language part from locale codes like tr-TR, en-EN, ru-RU
        // This matches the YAML file naming convention (Auth.tr.yaml, Auth.en.yaml, Auth.ru.yaml)
        return locale.Split('-')[0].ToLowerInvariant();
    }
}