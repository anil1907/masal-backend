using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Application.Services.Store;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Adapters.Store;

/// Verifies a StoreKit 2 signed transaction (JWS) WITHOUT any Apple secret/key:
/// 1) the x5c certificate chain in the JWS header must terminate at Apple Root CA - G3,
/// 2) the ES256 signature over header.payload must be valid for the leaf certificate,
/// 3) the decoded payload's bundle id must match this app.
/// This closes the entitlement-bypass risk (a forged transaction can no longer grant premium).
public class AppleStoreVerifier : IStoreVerifier
{
    private const string ExpectedBundleId = "com.anilyildirim.masal";

    // Apple Root CA - G3 (DER, base64). Public, stable:
    // https://www.apple.com/certificateauthority/AppleRootCA-G3.cer
    private const string AppleRootCaG3Der =
        "MIICQzCCAcmgAwIBAgIILcX8iNLFS5UwCgYIKoZIzj0EAwMwZzEbMBkGA1UEAwwSQXBwbGUgUm9vdCBDQSAtIEcz" +
        "MSYwJAYDVQQLDB1BcHBsZSBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTETMBEGA1UECgwKQXBwbGUgSW5jLjELMAkG" +
        "A1UEBhMCVVMwHhcNMTQwNDMwMTgxOTA2WhcNMzkwNDMwMTgxOTA2WjBnMRswGQYDVQQDDBJBcHBsZSBSb290IENB" +
        "IC0gRzMxJjAkBgNVBAsMHUFwcGxlIENlcnRpZmljYXRpb24gQXV0aG9yaXR5MRMwEQYDVQQKDApBcHBsZSBJbmMu" +
        "MQswCQYDVQQGEwJVUzB2MBAGByqGSM49AgEGBSuBBAAiA2IABJjpLz1AcqTtkyJygRMc3RCV8cWjTnHcFBbZDuWm" +
        "BSp3ZHtfTjjTuxxEtX/1H7YyYl3J6YRbTzBPEVoA/VhYDKX1DyxNB0cTddqXl5dvMVztK517IDvYuVTZXpmkOlEK" +
        "MaNCMEAwHQYDVR0OBBYEFLuw3qFYM4iapIqZ3r6966/ayySrMA8GA1UdEwEB/wQFMAMBAf8wDgYDVR0PAQH/BAQD" +
        "AgEGMAoGCCqGSM49BAMDA2gAMGUCMQCD6cHEFl4aXTQY2e3v9GwOAEZLuN+yRhHFD/3meoyhpmvOwgPUnPWTxnS4" +
        "at+qIxUCMG1mihDK1A3UT82NQz60imOlM27jbdoXt2QfyFMm+YhidDkLF1vLUagM6BgD56KyKA==";

    private readonly ILogger<AppleStoreVerifier> _logger;

    public AppleStoreVerifier(ILogger<AppleStoreVerifier> logger) => _logger = logger;

    public Task<StoreVerificationResult> VerifyAsync(
        string provider, string productId, string? signedTransaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signedTransaction))
            return Invalid("empty signed transaction");

        string[] parts = signedTransaction.Split('.');
        if (parts.Length != 3)
            return Invalid("malformed JWS");

        try
        {
            // --- certificate chain from the JWS header (x5c: [leaf, intermediate, root]) ---
            string leafB64, intermediateB64;
            using (JsonDocument header = JsonDocument.Parse(Base64UrlDecode(parts[0])))
            {
                JsonElement x5c = header.RootElement.GetProperty("x5c");
                if (x5c.GetArrayLength() < 2) return Invalid("missing certificate chain");
                leafB64 = x5c[0].GetString() ?? "";
                intermediateB64 = x5c[1].GetString() ?? "";
            }

            using var leaf = new X509Certificate2(Convert.FromBase64String(leafB64));
            using var intermediate = new X509Certificate2(Convert.FromBase64String(intermediateB64));
            using var appleRoot = new X509Certificate2(Convert.FromBase64String(AppleRootCaG3Der));

            // 1) chain must terminate at Apple Root CA - G3
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(appleRoot);
                chain.ChainPolicy.ExtraStore.Add(intermediate);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                if (!chain.Build(leaf))
                    return Invalid("certificate chain does not terminate at Apple Root CA - G3");
            }

            // 2) ES256 signature over "header.payload" using the leaf's public key
            using (ECDsa? ecdsa = leaf.GetECDsaPublicKey())
            {
                if (ecdsa is null) return Invalid("leaf has no EC public key");
                byte[] signed = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
                byte[] signature = Base64UrlDecode(parts[2]);
                if (!ecdsa.VerifyData(signed, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                    return Invalid("signature verification failed");
            }

            // 3) payload claims
            using JsonDocument payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            JsonElement root = payload.RootElement;

            string bundleId = root.TryGetProperty("bundleId", out JsonElement b) ? b.GetString() ?? "" : "";
            if (!string.Equals(bundleId, ExpectedBundleId, StringComparison.Ordinal))
                return Invalid($"bundle id mismatch ({bundleId})");

            string verifiedProductId = root.TryGetProperty("productId", out JsonElement p) ? p.GetString() ?? "" : "";

            DateTime? expiresAtUtc = null;
            if (root.TryGetProperty("expiresDate", out JsonElement e) && e.TryGetInt64(out long expiresMs))
                expiresAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(expiresMs).UtcDateTime;

            return Task.FromResult(new StoreVerificationResult(
                Valid: true,
                ProductId: string.IsNullOrEmpty(verifiedProductId) ? productId : verifiedProductId,
                ExpiresAtUtc: expiresAtUtc));
        }
        catch (Exception ex)
        {
            return Invalid($"verification error: {ex.Message}");
        }
    }

    private Task<StoreVerificationResult> Invalid(string reason)
    {
        _logger.LogWarning("[AppleStore] Purchase verification rejected: {Reason}", reason);
        return Task.FromResult(new StoreVerificationResult(false, "", null));
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
