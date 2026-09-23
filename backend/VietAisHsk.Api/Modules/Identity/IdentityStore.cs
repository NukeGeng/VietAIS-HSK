using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Identity;

public interface IIdentityStore
{
    IdentitySnapshot EnsureUserProvisioned(string userId);
    IdentitySnapshot UpdateProfile(string userId, UpdateLearnerProfileRequest request);
    IdentitySnapshot SetLearningTarget(string userId, SetLearningTargetRequest request);
    IReadOnlySet<string> GetPermissions(string userId);
    IReadOnlyList<UserListItem> SearchUsers(string? search);
    UserListItem? GetUser(string userId);
    UserListItem? ChangeStatus(string userId, UserStatus status);
}

public sealed class InMemoryIdentityStore : IIdentityStore
{
    private readonly ConcurrentDictionary<string, IdentitySnapshot> snapshots = new(StringComparer.Ordinal);

    public IdentitySnapshot EnsureUserProvisioned(string userId)
    {
        return snapshots.GetOrAdd(userId, static id =>
        {
            var now = DateTimeOffset.UtcNow;
            var account = new UserAccount(id, UserStatus.Active, now, now);
            var profile = new LearnerProfile(
                id,
                DisplayName: null,
                AvatarUrl: null,
                PreferredHskLevelId: null,
                TargetHskLevelId: null,
                Timezone: "UTC",
                StudyPreferences: new StudyPreferences(),
                UpdatedAt: now);
            var authorization = new AuthorizationContext(id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return new IdentitySnapshot(account, profile, authorization);
        });
    }

    public IdentitySnapshot UpdateProfile(string userId, UpdateLearnerProfileRequest request)
    {
        return snapshots.AddOrUpdate(
            userId,
            _ => throw new InvalidOperationException("User must be provisioned before profile update."),
            (_, current) =>
            {
                var now = DateTimeOffset.UtcNow;
                var profile = current.Profile with
                {
                    DisplayName = request.DisplayName?.Trim(),
                    AvatarUrl = request.AvatarUrl?.Trim(),
                    Timezone = request.Timezone?.Trim() ?? current.Profile.Timezone,
                    StudyPreferences = request.StudyPreferences ?? current.Profile.StudyPreferences,
                    UpdatedAt = now
                };
                return current with
                {
                    Profile = profile,
                    Account = current.Account with { UpdatedAt = now }
                };
            });
    }

    public IdentitySnapshot SetLearningTarget(string userId, SetLearningTargetRequest request)
    {
        return snapshots.AddOrUpdate(
            userId,
            _ => throw new InvalidOperationException("User must be provisioned before target update."),
            (_, current) =>
            {
                var now = DateTimeOffset.UtcNow;
                var profile = current.Profile with
                {
                    PreferredHskLevelId = request.PreferredHskLevelId?.Trim(),
                    TargetHskLevelId = request.TargetHskLevelId?.Trim(),
                    UpdatedAt = now
                };
                return current with
                {
                    Profile = profile,
                    Account = current.Account with { UpdatedAt = now }
                };
            });
    }

    public IReadOnlySet<string> GetPermissions(string userId)
    {
        return EnsureUserProvisioned(userId).Authorization.Permissions;
    }

    public IReadOnlyList<UserListItem> SearchUsers(string? search)
    {
        var normalized = search?.Trim();
        return snapshots.Values
            .Where(snapshot => string.IsNullOrWhiteSpace(normalized)
                || snapshot.Account.UserId.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || snapshot.Profile.DisplayName?.Contains(normalized, StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(snapshot => snapshot.Account.UserId, StringComparer.Ordinal)
            .Select(ToListItem)
            .ToArray();
    }

    public UserListItem? GetUser(string userId)
    {
        return snapshots.TryGetValue(userId, out var snapshot) ? ToListItem(snapshot) : null;
    }

    public UserListItem? ChangeStatus(string userId, UserStatus status)
    {
        if (!snapshots.TryGetValue(userId, out var current))
        {
            return null;
        }

        var updated = current with
        {
            Account = current.Account with { Status = status, UpdatedAt = DateTimeOffset.UtcNow }
        };
        snapshots[userId] = updated;
        return ToListItem(updated);
    }

    private static UserListItem ToListItem(IdentitySnapshot snapshot) => new(
        snapshot.Account.UserId,
        snapshot.Account.Status,
        snapshot.Profile.DisplayName,
        snapshot.Profile.TargetHskLevelId,
        snapshot.Account.UpdatedAt);
}
