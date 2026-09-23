using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Identity;

namespace VietAisHsk.Api.Modules.Progress;

public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/progress");

        group.MapGet("", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetSnapshot(userId))));

        group.MapGet("/mastery", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetMastery(userId))));

        group.MapGet("/weak-points", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetWeakPoints(userId))));

        group.MapGet("/history", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetHistory(userId))));

        group.MapGet("/streak", (IUserContextAccessor contextAccessor, IIdentityStore identityStore, IProgressStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var timezone = identityStore.EnsureUserProvisioned(context.UserId).Profile.Timezone;
            return Results.Ok(store.GetStreak(context.UserId, timezone));
        });

        return endpoints;
    }

    private static IResult WithLearner(IUserContextAccessor contextAccessor, Func<string, IResult> handler)
    {
        var context = contextAccessor.Current;
        return context is null ? Results.Unauthorized() : handler(context.UserId);
    }
}
