namespace Application.Features.Children.Constants;

public static class ChildBusinessMessages
{
    // Must match the feature FOLDER name (the YAML localization loader keys sections by folder).
    public const string SectionName = "Children";

    public const string ChildAlreadyExists = "ChildAlreadyExists";
    public const string ChildNotExists = "ChildNotExists";
    public const string FreeChildLimitReached = "FreeChildLimitReached";
    public const string PremiumChildLimitReached = "PremiumChildLimitReached";
}
