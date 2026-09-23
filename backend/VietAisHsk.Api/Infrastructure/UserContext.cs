using System.Security.Claims;
using System.Text.Json;

namespace VietAisHsk.Api.Infrastructure;

public sealed record UserContext(string UserId, IReadOnlySet<string> Permissions);

public interface IUserContextAccessor
{
    UserContext? Current { get; }
}

public sealed class HttpUserContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment) : IUserContextAccessor
{
    public UserContext? Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? httpContext.User.FindFirstValue("sub");

            // Development-only adapter for the local Computer Use and integration flows.
            // Production authentication must populate trusted claims through the selected provider.
            if (hostEnvironment.IsDevelopment() && string.IsNullOrWhiteSpace(userId))
            {
                userId = httpContext.Request.Headers["X-Dev-User-Id"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var permissions = httpContext.User.Claims
                .Where(claim => claim.Type is "permission" or "permissions")
                .SelectMany(claim => ExpandPermissions(claim.Value))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (hostEnvironment.IsDevelopment())
            {
                var developmentPermissions = httpContext.Request.Headers["X-Dev-Permission"].ToString();
                foreach (var value in developmentPermissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    permissions.Add(value);
                }
            }

            return new UserContext(userId.Trim(), permissions);
        }
    }

    private static IEnumerable<string> ExpandPermissions(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                return JsonSerializer.Deserialize<string[]>(trimmed)
                    ?.Where(permission => !string.IsNullOrWhiteSpace(permission))
                    .Select(permission => permission.Trim())
                    ?? [];
            }
            catch (JsonException)
            {
                // Fall through to the simple delimiter format for a malformed provider claim.
            }
        }

        return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
