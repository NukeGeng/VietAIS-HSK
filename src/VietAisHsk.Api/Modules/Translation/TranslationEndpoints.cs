using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Modules.Translation;

public static class TranslationEndpoints
{
    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/translation");

        group.MapGet("/exercises", (ITranslationCatalog catalog) => Results.Ok(catalog.GetExercises().Select(ToExerciseView)));

        group.MapPost("/exercises/{id}/attempts", (string id, SubmitTranslationAttemptRequest request, IUserContextAccessor contextAccessor, ITranslationCatalog catalog, ITranslationStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.AnswerChinese))
            {
                return Results.Problem("AnswerChinese là bắt buộc.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var exercise = catalog.GetExercise(id);
            return exercise is null
                ? Results.NotFound()
                : Results.Created($"/api/translation/attempts/{{id}}", ToAttemptView(store.Create(context.UserId, exercise, request.AnswerChinese.Trim())));
        });

        group.MapPost("/attempts/{id}/feedback", async (string id, IUserContextAccessor contextAccessor, ITranslationStore store, ITranslationFeedbackGateway feedbackGateway, CancellationToken cancellationToken) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var attempt = store.Get(context.UserId, id);
            if (attempt is null)
            {
                return Results.NotFound();
            }

            if (attempt.Feedback is not null)
            {
                return Results.Ok(ToAttemptView(attempt));
            }

            var feedback = await feedbackGateway.RequestAsync(attempt, cancellationToken);
            return feedback is null
                ? Results.Problem("AI feedback provider chưa được cấu hình.", statusCode: StatusCodes.Status503ServiceUnavailable, title: "Feedback unavailable")
                : Results.Ok(ToAttemptView(store.SetFeedback(context.UserId, id, feedback)!));
        });

        group.MapGet("/history", (IUserContextAccessor contextAccessor, ITranslationStore store) =>
        {
            var context = contextAccessor.Current;
            return context is null ? Results.Unauthorized() : Results.Ok(store.GetHistory(context.UserId).Select(ToAttemptView));
        });

        return endpoints;
    }

    private static TranslationExerciseView ToExerciseView(TranslationExercise exercise) =>
        new(exercise.Id, exercise.PromptVietnamese, exercise.HskContext, exercise.Status);

    private static TranslationAttemptView ToAttemptView(TranslationAttempt attempt) =>
        new(attempt.Id, attempt.ExerciseId, attempt.AnswerChinese, attempt.SubmittedAt, attempt.Feedback);
}
