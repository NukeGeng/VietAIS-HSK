using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Modules.Review;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/review");

        group.MapGet("/summary", (IUserContextAccessor contextAccessor, IReviewStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetSummary(userId))));

        group.MapGet("/items", (IUserContextAccessor contextAccessor, IReviewStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetItems(userId))));

        group.MapGet("/needs-review", (IUserContextAccessor contextAccessor, IReviewStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetItems(userId, dueOnly: true))));

        group.MapGet("/mistakes", (IUserContextAccessor contextAccessor, IReviewStore store) =>
            WithLearner(contextAccessor, userId => Results.Ok(store.GetItems(userId, mistakesOnly: true))));

        group.MapPost("/sessions", (StartReviewSessionRequest request, IUserContextAccessor contextAccessor, IReviewStore store) =>
        {
            if (contextAccessor.Current is null)
            {
                return Results.Unauthorized();
            }

            if (request.ItemIds is null || request.ItemIds.Count == 0)
            {
                return Results.Problem("ItemIds là bắt buộc.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var session = store.StartSession(contextAccessor.Current.UserId, request.ItemIds);
            return session is null ? Results.NotFound() : Results.Created($"/api/review/sessions/{session.Id}", session);
        });

        group.MapPost("/sessions/{id}/results", (string id, RecordReviewResultRequest request, IUserContextAccessor contextAccessor, IReviewStore store) =>
        {
            if (contextAccessor.Current is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.ItemId))
            {
                return Results.Problem("ItemId là bắt buộc.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var item = store.RecordResult(contextAccessor.Current.UserId, id, request.ItemId.Trim(), request.Correct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        return endpoints;
    }

    private static IResult WithLearner(IUserContextAccessor contextAccessor, Func<string, IResult> handler)
    {
        var context = contextAccessor.Current;
        return context is null ? Results.Unauthorized() : handler(context.UserId);
    }
}
