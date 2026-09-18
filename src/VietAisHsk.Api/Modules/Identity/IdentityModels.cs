namespace VietAisHsk.Api.Modules.Identity;

public enum UserStatus
{
    Active,
    Suspended
}

public sealed record UserAccount(
    string UserId,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LearnerProfile(
    string UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? PreferredHskLevelId,
    string? TargetHskLevelId,
    string Timezone,
    StudyPreferences StudyPreferences,
    DateTimeOffset UpdatedAt);

public sealed record StudyPreferences(
    int DailyMinutes = 20,
    string? PreferredStudyTime = null);

public sealed record AuthorizationContext(
    string UserId,
    IReadOnlySet<string> Permissions);

public sealed record IdentitySnapshot(
    UserAccount Account,
    LearnerProfile Profile,
    AuthorizationContext Authorization);

public sealed record UpdateLearnerProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    string? Timezone,
    StudyPreferences? StudyPreferences);

public sealed record SetLearningTargetRequest(
    string? PreferredHskLevelId,
    string? TargetHskLevelId);

public sealed record ChangeUserStatusRequest(UserStatus Status);

public sealed record UserListItem(
    string UserId,
    UserStatus Status,
    string? DisplayName,
    string? TargetHskLevelId,
    DateTimeOffset UpdatedAt);
