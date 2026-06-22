namespace Application.Features.Children.Constants;

public static class ChildConstants
{
    /// Free accounts get a single child; premium unlocks the whole family.
    public const int MaxFreeChildren = 1;

    /// Premium cap - keeps per-account TTS cost predictable while covering most families.
    public const int MaxPremiumChildren = 3;
}
