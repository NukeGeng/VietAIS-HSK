using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Curriculum;
using VietAisHsk.Api.Modules.Progress;

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

        group.MapPost("/beginner/stages/{id}/start", (string id, IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum) =>
        {
            var stage = curriculum.GetBeginnerTrack().Stages.FirstOrDefault(candidate =>
                candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (stage is null || stage.Status != ContentStatus.Published)
            {
                return Results.NotFound();
            }

            return WithLearner(contextAccessor, store, state =>
            {
                if (state.CurrentTrack is null)
                {
                    if (!stage.Id.Equals("pinyin", StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.Conflict("Hãy học các bước trước theo đúng thứ tự.");
                    }

                    return Results.Ok(store.StartBeginner(state.UserId));
                }

                if (state.CurrentTrack != "beginner"
                    || !stage.Id.Equals(state.CurrentBeginnerStageId, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Conflict("Hãy học các bước trước theo đúng thứ tự.");
                }

                var updated = store.StartBeginnerStage(state.UserId, stage.Id);
                return Results.Ok(updated ?? state);
            });
        });

        group.MapPost("/beginner/stages/{id}/complete", (string id, IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum, IProgressSignalSink progressSignals) =>
        {
            var stages = curriculum.GetBeginnerTrack().Stages;
            var stageIndex = stages.ToList().FindIndex(candidate =>
                candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (stageIndex < 0 || stages[stageIndex].Status != ContentStatus.Published)
            {
                return Results.NotFound();
            }

            var nextStageId = stages.Skip(stageIndex + 1)
                .FirstOrDefault(candidate => candidate.Status == ContentStatus.Published)?.Id;
            return WithLearner(contextAccessor, store, state =>
            {
                if (state.CompletedBeginnerStageIds.Contains(stages[stageIndex].Id))
                {
                    return Results.Ok(state);
                }
                if (state.CurrentTrack != "beginner"
                    || state.CurrentBeginnerStageId != stages[stageIndex].Id
                    || !state.StartedBeginnerStageIds.Contains(stages[stageIndex].Id))
                {
                    return Results.Conflict("Hãy bắt đầu đúng bước hiện tại trước khi hoàn tất.");
                }
                var updated = store.CompleteBeginnerStage(state.UserId, stages[stageIndex].Id, nextStageId);
                if (updated is null)
                {
                    return Results.Conflict("Hãy bắt đầu đúng bước hiện tại trước khi hoàn tất.");
                }

                progressSignals.RecordActivity(state.UserId, "beginner-stage-completed", stages[stageIndex].Id, DateTimeOffset.UtcNow);
                return Results.Ok(updated);
            });
        });

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

        group.MapPost("/lessons/{id}/complete", (string id, IUserContextAccessor contextAccessor, ILearningStore store, ICurriculumStore curriculum, IProgressSignalSink progressSignals) =>
        {
            if (!curriculum.IsPublishedLesson(id))
            {
                return Results.NotFound();
            }

            return WithLearner(contextAccessor, store, state =>
            {
                if (state.CompletedLessonIds.Contains(id))
                {
                    return Results.Ok(state);
                }
                if (!state.StartedLessonIds.Contains(id))
                {
                    return Results.Problem("Bài học phải được bắt đầu trước khi hoàn thành.", statusCode: StatusCodes.Status409Conflict, title: "Business rule");
                }
                var updated = store.CompleteLesson(state.UserId, id);
                if (updated is null)
                {
                    return Results.Problem("Bài học phải được bắt đầu trước khi hoàn thành.", statusCode: StatusCodes.Status409Conflict, title: "Business rule");
                }

                progressSignals.RecordActivity(state.UserId, "lesson-completed", id, DateTimeOffset.UtcNow);
                return Results.Ok(updated);
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
        state.CurrentTrack switch
        {
            "beginner" when state.CurrentBeginnerStageId is not null =>
                $"beginner/{state.CurrentBeginnerStageId}",
            "hsk" => state.CurrentLessonId,
            _ => null
        };
}
