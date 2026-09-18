using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Modules.Learning;

public static class LearningEndpoints
{
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/learning");

        group.MapGet("/home", (IUserContextAccessor contextAccessor, ILearningStore store) =>
            WithLearner(contextAccessor, store, state => Results.Ok(new LearningHome(state, GetContinueTarget(state)))));

        group.MapGet("/beginner", (IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum) =>
            WithLearner(contextAccessor, store, state => Results.Ok(new BeginnerLearningView(curriculum.GetBeginnerTrack(), state))));

        group.MapPost("/beginner/start", (IUserContextAccessor contextAccessor, ILearningStore store) =>
            WithLearner(contextAccessor, store, state => Results.Ok(store.StartBeginner(state.UserId))));

        group.MapGet("/hsk/{level}", (string level, IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum) =>
        {
            var tree = curriculum.GetPublishedTree(level);
            if (tree is null)
            {
                return Results.NotFound();
            }

            return WithLearner(contextAccessor, store, state => Results.Ok(new HskLearningView(tree, state)));
        });

        group.MapPost("/hsk/{level}/select", (string level, IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum) =>
        {
            if (curriculum.GetPublishedTree(level) is null)
            {
                return Results.NotFound();
            }

            return WithLearner(contextAccessor, store, state =>
                Results.Ok(store.SelectHsk(state.UserId, level)));
        });

        group.MapPost("/lessons/{id}/start", (string id, IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum) =>
        {
            if (!curriculum.IsPublishedLesson(id))
            {
                return Results.NotFound();
            }

            return WithLearner(contextAccessor, store, state =>
                Results.Ok(store.StartLesson(state.UserId, id)));
        });

        group.MapPost("/lessons/{id}/complete", (string id, IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum) =>
        {
            if (!curriculum.IsPublishedLesson(id))
            {
                return Results.NotFound();
            }

            return WithLearner(contextAccessor, store, state =>
            {
                var updated = store.CompleteLesson(state.UserId, id);
                return updated is null
                    ? Results.Problem("Bài học phải được bắt đầu trước khi hoàn thành.", statusCode: StatusCodes.Status409Conflict, title: "Business rule")
                    : Results.Ok(updated);
            });
        });

        return endpoints;
    }

    private static IResult WithLearner(
        IUserContextAccessor contextAccessor,
        ILearningStore store,
        Func<LearningState, IResult> handler)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return Results.Unauthorized();
        }

        var state = store.GetOrCreate(context.UserId);
        return handler(state);
    }

    private static string? GetContinueTarget(LearningState state) =>
        state.CurrentLessonId
        ?? (state.CurrentTrack == "beginner" ? $"beginner/{state.CurrentBeginnerStageId}" : null);
}
