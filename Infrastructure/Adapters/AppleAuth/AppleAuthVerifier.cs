using Application.Services.AppleAuth;
using Core.CrossCuttingConcerns.Exception.Types;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Adapters.AppleAuth;

/// Validates Apple identity tokens against Apple's published JWKS
/// (https://appleid.apple.com/auth/keys). Keys are cached (they rotate rarely).
/// No Apple secret/key is needed for the native iOS flow - verification uses public keys.
public class AppleAuthVerifier : IAppleAuthVerifier
{
    private readonly HttpClient _http;
    private readonly AppleAuthOptions _options;

    // Apple's signing keys are stable for long stretches; cache across requests.
    private static IList<SecurityKey>? _cachedKeys;
    private static DateTime _cachedAtUtc;
    private static readonly SemaphoreSlim _keysLock = new(1, 1);
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromHours(6);

    public AppleAuthVerifier(HttpClient http, IOptions<AppleAuthOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<AppleIdentity> VerifyAsync(string identityToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityToken))
            throw new BusinessException("Apple kimlik belirteci boş.");

        IList<SecurityKey> keys = await GetSigningKeysAsync(cancellationToken);

        var handler = new JsonWebTokenHandler();
        TokenValidationResult result = await handler.ValidateTokenAsync(identityToken, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidateIssuer = true,
            ValidAudience = _options.Audience,
            ValidateAudience = true,
            IssuerSigningKeys = keys,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        });

        if (!result.IsValid)
            throw new BusinessException("Apple kimlik doğrulaması başarısız.");

        string? sub = result.ClaimsIdentity.FindFirst("sub")?.Value;
        string? email = result.ClaimsIdentity.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(sub))
            throw new BusinessException("Apple kimliği okunamadı.");

        return new AppleIdentity(sub, email);
    }

    private async Task<IList<SecurityKey>> GetSigningKeysAsync(CancellationToken cancellationToken)
    {
        if (_cachedKeys is not null && DateTime.UtcNow - _cachedAtUtc < _cacheTtl)
            return _cachedKeys;

        await _keysLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedKeys is not null && DateTime.UtcNow - _cachedAtUtc < _cacheTtl)
                return _cachedKeys;

            string json = await _http.GetStringAsync("/auth/keys", cancellationToken);
            var jwks = new JsonWebKeySet(json);
            _cachedKeys = jwks.GetSigningKeys();
            _cachedAtUtc = DateTime.UtcNow;
            return _cachedKeys;
        }
        finally
        {
            _keysLock.Release();
        }
    }
}
