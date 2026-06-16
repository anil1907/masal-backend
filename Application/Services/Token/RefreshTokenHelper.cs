using System.Security.Cryptography;
using System.Text;

namespace Application.Services.Token;

/// Generates and hashes opaque refresh tokens. Raw value goes to the client once;
/// only the SHA-256 hash is persisted.
public static class RefreshTokenHelper
{
    public static (string Raw, string Hash) Generate()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);
        string raw = Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');   // url-safe
        return (raw, Hash(raw));
    }

    public static string Hash(string raw)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(hash);
    }
}
