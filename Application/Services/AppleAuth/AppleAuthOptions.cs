namespace Application.Services.AppleAuth;

/// Bound from the "AppleAuth" config section. Audience must equal the iOS app bundle id
/// (the `aud` claim Apple puts in the identity token for the native flow).
public class AppleAuthOptions
{
    public string Audience { get; set; } = "com.anilyildirim.masal";
    public string Issuer { get; set; } = "https://appleid.apple.com";
}
