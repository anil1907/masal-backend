using Application.Services.Store;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Adapters.Store;

/// DEV ONLY: trusts the client and grants a one-month period. Lets the purchase -> entitlement
/// flow be exercised without App Store Server verification. Production must swap this for an
/// AppleStoreVerifier that validates the signed transaction against Apple before granting.
public class DevStoreVerifier : IStoreVerifier
{
    private readonly ILogger<DevStoreVerifier> _logger;

    public DevStoreVerifier(ILogger<DevStoreVerifier> logger) => _logger = logger;

    public Task<StoreVerificationResult> VerifyAsync(
        string provider, string productId, string? transactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[DEV STORE] Trusting unverified purchase (provider={Provider}, productId={ProductId}, txn={Txn}). " +
            "Do NOT use in production.", provider, productId, transactionId ?? "-");

        // Monthly subscription -> one period from now.
        var expires = DateTime.UtcNow.AddMonths(1);
        return Task.FromResult(new StoreVerificationResult(Valid: true, ProductId: productId, ExpiresAtUtc: expires));
    }
}
