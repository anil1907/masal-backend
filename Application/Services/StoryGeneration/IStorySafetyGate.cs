namespace Application.Services.StoryGeneration;

/// Verdict from the child-safety gate. A story is published ONLY when Passed is true.
public record SafetyVerdict(bool Passed, string Reason);

/// Separate, post-hoc safety check on generated story TEXT (not just a prompt instruction).
/// The hardest rule of the product: nothing unsafe reaches a child.
public interface IStorySafetyGate
{
    Task<SafetyVerdict> EvaluateAsync(
        string storyText,
        IReadOnlyList<string> childFears,
        string? ageBand,
        CancellationToken cancellationToken = default);
}
