namespace Application.Services.StoryGeneration;

/// Bound from the "Anthropic" config section. ApiKey comes from user-secrets (dev)
/// or the host env var Anthropic__ApiKey (prod) - never from source/appsettings.
public class AnthropicOptions
{
    public string ApiKey { get; set; } = "";
    /// Story model. Haiku 4.5 is cheap and plenty for bedtime stories; swap to Sonnet for richer prose.
    public string Model { get; set; } = "claude-haiku-4-5-20251001";
    /// Lightweight model for the safety gate classification.
    public string SafetyModel { get; set; } = "claude-haiku-4-5-20251001";
}
