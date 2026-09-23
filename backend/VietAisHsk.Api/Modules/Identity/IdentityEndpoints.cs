using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Modules.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/me", (IUserContextAccessor contextAccessor, IIdentityStore store) =>
            WithLearner(contextAccessor, store, snapshot => Results.Ok(snapshot)));

        group.MapGet("/me/authorization", (IUserContextAccessor contextAccessor, IIdentityStore store) =>
            WithLearner(contextAccessor, store, snapshot => Results.Ok(snapshot.Authorization)));

        group.MapPut("/me/profile", (UpdateLearnerProfileRequest request, IUserContextAccessor contextAccessor, IIdentityStore store) =>
        {
            var validationError = IdentityValidation.ValidateProfile(request);
            if (validationError is not null)
            {
                return Results.Problem(validationError, statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            return WithLearner(contextAccessor, store, snapshot =>
            {
                var updated = store.UpdateProfile(snapshot.Account.UserId, request);
                return Results.Ok(updated.Profile);
            });
        });

        group.MapPut("/me/learning-target", (SetLearningTargetRequest request, IUserContextAccessor contextAccessor, IIdentityStore store) =>
        {
            var validationError = IdentityValidation.ValidateLearningTarget(request);
            if (validationError is not null)
            {
                return Results.Problem(validationError, statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            return WithLearner(contextAccessor, store, snapshot =>
            {
                var updated = store.SetLearningTarget(snapshot.Account.UserId, request);
                return Results.Ok(updated.Profile);
            });
        });

        group.MapGet("/admin/users", (string? search, IUserContextAccessor contextAccessor, IIdentityStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "users.manage"))
            {
                return Forbidden();
            }

            return Results.Ok(store.SearchUsers(search));
        });

        group.MapGet("/admin/users/{id}", (string id, IUserContextAccessor contextAccessor, IIdentityStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "users.manage"))
            {
                return Forbidden();
            }

            var user = store.GetUser(id);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        group.MapPatch("/admin/users/{id}/status", (string id, ChangeUserStatusRequest request, IUserContextAccessor contextAccessor, IIdentityStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "users.manage"))
            {
                return Forbidden();
            }

            var user = store.ChangeStatus(id, request.Status);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        return endpoints;
    }

    private static IResult WithLearner(
        IUserContextAccessor contextAccessor,
        IIdentityStore store,
        Func<IdentitySnapshot, IResult> handler)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return Results.Unauthorized();
        }

        var snapshot = store.EnsureUserProvisioned(context.UserId);
        if (snapshot.Account.Status == UserStatus.Suspended)
        {
            return Results.Problem("Tài khoản đang bị khóa.", statusCode: StatusCodes.Status403Forbidden, title: "Account suspended");
        }

        // Authorization is request-scoped. Keep the persisted snapshot for account/profile data,
        // but expose the permissions resolved from trusted claims (or the development adapter).
        return handler(snapshot with
        {
            Authorization = new AuthorizationContext(context.UserId, context.Permissions)
        });
    }

    private static bool HasPermission(UserContext? context, string permission) =>
        context?.Permissions.Contains(permission) == true;

    private static IResult Forbidden() => Results.Problem(
        "Bạn không có permission cần thiết cho thao tác này.",
        statusCode: StatusCodes.Status403Forbidden,
        title: "Forbidden");
}
