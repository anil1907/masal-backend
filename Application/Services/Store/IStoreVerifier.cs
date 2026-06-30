namespace Application.Services.Store;

public record StoreVerificationResult(bool Valid, string ProductId, DateTime? ExpiresAtUtc);

/// Verifies a store purchase/subscription before it is trusted as a server entitlement.
/// For Apple, the client sends the StoreKit 2 signed transaction (JWS), which is verified
/// against Apple's root CA - no secret/key needed.
public interface IStoreVerifier
{
    Task<StoreVerificationResult> VerifyAsync(
        string provider, string productId, string? signedTransaction, CancellationToken cancellationToken = default);
}
