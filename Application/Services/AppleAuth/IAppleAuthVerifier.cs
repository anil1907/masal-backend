namespace Application.Services.AppleAuth;

/// Verified identity from a Sign in with Apple token. UserId = Apple's stable `sub` claim.
public record AppleIdentity(string UserId, string? Email);

/// Validates an Apple identity token (native iOS flow) against Apple's public JWKS:
/// signature, issuer (appleid.apple.com), audience (our bundle id) and expiry.
public interface IAppleAuthVerifier
{
    Task<AppleIdentity> VerifyAsync(string identityToken, CancellationToken cancellationToken = default);
}
