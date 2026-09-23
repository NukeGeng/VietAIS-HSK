using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Identity;

namespace VietAisHsk.Api.Tests;

public sealed class IdentityStoreTests
{
    [Fact]
    public void First_login_provisioning_is_idempotent_and_profile_updates_stay_scoped()
    {
        var store = new InMemoryIdentityStore();

        var first = store.EnsureUserProvisioned("learner-1");
        var provisionedAgain = store.EnsureUserProvisioned("learner-1");
        store.UpdateProfile("learner-1", new UpdateLearnerProfileRequest(
            "Học viên một",
            null,
            "Asia/Ho_Chi_Minh",
            new StudyPreferences(30, "20:00")));

        var updated = store.EnsureUserProvisioned("learner-1");
        var otherUser = store.EnsureUserProvisioned("learner-2");

        Assert.Equal(first, provisionedAgain);
        Assert.Equal("Học viên một", updated.Profile.DisplayName);
        Assert.Equal("Asia/Ho_Chi_Minh", updated.Profile.Timezone);
        Assert.Equal(30, updated.Profile.StudyPreferences.DailyMinutes);
        Assert.Null(otherUser.Profile.DisplayName);
        Assert.Equal("UTC", otherUser.Profile.Timezone);
    }

    [Fact]
    public void Admin_query_only_returns_users_that_have_been_provisioned()
    {
        var store = new InMemoryIdentityStore();
        store.EnsureUserProvisioned("learner-1");
        store.EnsureUserProvisioned("learner-2");

        var users = store.SearchUsers(null);

        Assert.Equal(["learner-1", "learner-2"], users.Select(user => user.UserId));
    }

    [Fact]
    public void Development_user_context_reads_request_scoped_permissions()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Dev-User-Id"] = "admin-1";
        httpContext.Request.Headers["X-Dev-Permission"] = "content.manage, users.manage";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var context = new HttpUserContextAccessor(httpContextAccessor, new DevelopmentHostEnvironment()).Current;

        Assert.NotNull(context);
        Assert.Equal("admin-1", context!.UserId);
        Assert.Contains("content.manage", context.Permissions);
        Assert.Contains("users.manage", context.Permissions);
    }

    [Fact]
    public void Trusted_jwt_claims_resolve_identity_and_permissions_without_dev_headers()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "jwt-user"),
                new Claim("permissions", "[\"content.manage\",\"users.manage\"]"),
                new Claim("permission", "curriculum.manage")
            ],
            authenticationType: "Bearer"))
        };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var environment = new DevelopmentHostEnvironment { EnvironmentName = Environments.Production };

        var context = new HttpUserContextAccessor(httpContextAccessor, environment).Current;

        Assert.NotNull(context);
        Assert.Equal("jwt-user", context!.UserId);
        Assert.Contains("content.manage", context.Permissions);
        Assert.Contains("users.manage", context.Permissions);
        Assert.Contains("curriculum.manage", context.Permissions);
    }

    private sealed class DevelopmentHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = typeof(IdentityStoreTests).Assembly.GetName().Name!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
