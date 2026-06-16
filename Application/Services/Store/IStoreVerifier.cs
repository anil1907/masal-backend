namespace Application.Services.Store;

public record StoreVerificationResult(bool Valid, string ProductId, DateTime? ExpiresAtUtc);

/// Verifies a store purchase/subscription before it is trusted as a server entitlement.
/// Provider-agnostic; the live implementation talks to Apple's App Store Server API.
/// Dev uses a trusting stub so the freemium -> premium flow works end to end.
public interface IStoreVerifier
{
    Task<StoreVerificationResult> VerifyAsync(
        string provider, string productId, string? transactionId, CancellationToken cancellationToken = default);
}
