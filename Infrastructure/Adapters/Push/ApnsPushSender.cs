using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Persistence;
using Application.Services.Push;
using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Adapters.Push;

/// Token-based (.p8) APNs sender over HTTP/2. Builds an ES256-signed JWT from the auth key,
/// caches it (Apple wants it refreshed at most ~once/20min, at least once/60min), and POSTs an
/// alert to each of the user's device tokens. Stale tokens (410/BadDeviceToken) are soft-deleted.
public class ApnsPushSender : IPushSender
{
    private const string ProductionHost = "https://api.push.apple.com";
    private const string SandboxHost = "https://api.sandbox.push.apple.com";

    private readonly HttpClient _http;
    private readonly IApplicationDbContext _db;
    private readonly ApnsOptions _options;
    private readonly ILogger<ApnsPushSender> _logger;

    // Cached provider JWT (one per process is fine - it is keyed only by KeyId/TeamId).
    private static readonly SemaphoreSlim JwtLock = new(1, 1);
    private static string? _cachedJwt;
    private static DateTime _cachedJwtAtUtc;

    public ApnsPushSender(
        HttpClient http,
        IApplicationDbContext db,
        IOptions<ApnsOptions> options,
        ILogger<ApnsPushSender> logger)
    {
        _http = http;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendToUserAsync(long userId, string title, string body, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("[APNs] Not configured - skipping push for user {UserId}.", userId);
            return;
        }

        List<DeviceToken> devices = await _db.DeviceTokens
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);
        if (devices.Count == 0)
            return;

        string jwt;
        try
        {
            jwt = await GetProviderJwtAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[APNs] Failed to build provider JWT - push aborted.");
            return;
        }

        string payload = JsonSerializer.Serialize(new
        {
            aps = new
            {
                alert = new { title, body },
                sound = "default"
            }
        });

        foreach (DeviceToken device in devices)
        {
            try
            {
                await SendOneAsync(device, payload, jwt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[APNs] Push to device {DeviceId} failed.", device.Id);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SendOneAsync(DeviceToken device, string payload, string jwt, CancellationToken cancellationToken)
    {
        string host = device.IsProduction ? ProductionHost : SandboxHost;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{host}/3/device/{device.Token}")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("authorization", $"bearer {jwt}");
        request.Headers.TryAddWithoutValidation("apns-topic", _options.BundleId);
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
        request.Headers.TryAddWithoutValidation("apns-priority", "10");

        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        string reason = await response.Content.ReadAsStringAsync(cancellationToken);
        // 410 Gone (Unregistered) or 400 BadDeviceToken => the token is dead; drop it.
        if (response.StatusCode == HttpStatusCode.Gone || reason.Contains("BadDeviceToken", StringComparison.Ordinal))
        {
            device.DeletedDate = DateTime.UtcNow;
            _db.DeviceTokens.Update(device);
            _logger.LogInformation("[APNs] Dropped stale device token {DeviceId} ({Status}).", device.Id, response.StatusCode);
        }
        else
        {
            _logger.LogWarning("[APNs] Push rejected for device {DeviceId}: {Status} {Reason}",
                device.Id, response.StatusCode, reason);
        }
    }

    /// ES256 JWT signed with the .p8 key; cached and refreshed every ~50 minutes.
    private async Task<string> GetProviderJwtAsync(CancellationToken cancellationToken)
    {
        await JwtLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedJwt is not null && (DateTime.UtcNow - _cachedJwtAtUtc) < TimeSpan.FromMinutes(50))
                return _cachedJwt;

            string header = Base64Url(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { alg = "ES256", kid = _options.KeyId })));
            long iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string claims = Base64Url(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { iss = _options.TeamId, iat })));
            string signingInput = $"{header}.{claims}";

            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_options.PrivateKey);
            byte[] signature = ecdsa.SignData(
                Encoding.ASCII.GetBytes(signingInput),
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            _cachedJwt = $"{signingInput}.{Base64Url(signature)}";
            _cachedJwtAtUtc = DateTime.UtcNow;
            return _cachedJwt;
        }
        finally
        {
            JwtLock.Release();
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
