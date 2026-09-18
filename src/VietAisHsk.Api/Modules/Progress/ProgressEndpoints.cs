using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Modules.Progress;

public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/progress");

        group.MapGet("", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetSnapshot(userId))));

        group.MapGet("/weak-points", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetWeakPoints(userId))));

        group.MapGet("/history", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetHistory(userId))));

        group.MapGet("/streak", (IUserContextAccessor contextAccessor, IProgressStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetStreak(userId, "UTC"))));

        return endpoints;
    }

    private static IResult WithLearner(IUserContextAccessor contextAccessor, Func<string, IResult> handler)
    {
        var context = contextAccessor.Current;
        return context is null ? Results.Unauthorized() : handler(context.UserId);
    }
}
