namespace Application.Features.Stories.Constants;

public static class StoryBusinessMessages
{
    // Must match the feature FOLDER name (the YAML localization loader keys sections by folder).
    public const string SectionName = "Stories";

    public const string DailyGenerationLimitExceeded = "DailyGenerationLimitExceeded";
    public const string StoryFailedSafetyCheck = "StoryFailedSafetyCheck";
    public const string ChapterNotFound = "ChapterNotFound";
}
