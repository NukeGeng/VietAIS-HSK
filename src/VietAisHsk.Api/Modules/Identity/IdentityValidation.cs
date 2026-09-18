namespace VietAisHsk.Api.Modules.Identity;

public static class IdentityValidation
{
    private static readonly HashSet<string> SupportedHskReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        "BEGINNER", "HSK1", "HSK2", "HSK3", "HSK4", "HSK5", "HSK6"
    };

    public static string? ValidateProfile(UpdateLearnerProfileRequest request)
    {
        if (request.DisplayName is { Length: > 120 })
        {
            return "DisplayName không được dài hơn 120 ký tự.";
        }

        if (request.AvatarUrl is { Length: > 500 })
        {
            return "AvatarUrl không được dài hơn 500 ký tự.";
        }

        if (request.Timezone is not null && !IsValidTimezone(request.Timezone))
        {
            return "Timezone không hợp lệ.";
        }

        if (request.StudyPreferences is { DailyMinutes: < 5 or > 240 })
        {
            return "DailyMinutes phải nằm trong khoảng 5 đến 240 phút.";
        }

        return null;
    }

    public static string? ValidateLearningTarget(SetLearningTargetRequest request)
    {
        if (request.PreferredHskLevelId is not null && !IsSupportedHsk(request.PreferredHskLevelId))
        {
            return "PreferredHskLevelId không tồn tại trong danh sách HSK hiện có.";
        }

        if (request.TargetHskLevelId is not null && !IsSupportedHsk(request.TargetHskLevelId))
        {
            return "TargetHskLevelId không tồn tại trong danh sách HSK hiện có.";
        }

        return null;
    }

    private static bool IsSupportedHsk(string value) => SupportedHskReferences.Contains(value.Trim());

    private static bool IsValidTimezone(string value)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
