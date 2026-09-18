using System.Security.Claims;

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

            var permissions = httpContext.User.FindAll("permission")
                .Select(claim => claim.Value)
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
}
