using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Translation;

public static class TranslationEndpoints
{
    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/translation");

        group.MapGet("/exercises", (ITranslationCatalog catalog) => Results.Ok(catalog.GetExercises().Select(ToExerciseView)));

        group.MapPost("/exercises/{id}/attempts", (string id, SubmitTranslationAttemptRequest request, IUserContextAccessor contextAccessor, ITranslationCatalog catalog, ITranslationStore store, IProgressSignalSink progressSignals) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var answerValidationError = TranslationValidation.ValidateAnswer(request.AnswerChinese);
            if (answerValidationError is not null)
            {
                return Results.Problem(answerValidationError, statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var answer = request.AnswerChinese!.Trim();

            var exercise = catalog.GetExercise(id);
            if (exercise is null)
            {
                return Results.NotFound();
            }

            var attempt = store.Create(context.UserId, exercise, answer);
            progressSignals.ApplyTranslationSignal(new TranslationAttemptSignal(
                context.UserId,
                attempt.Id,
                attempt.ExerciseId,
                attempt.SubmittedAt,
                $"translation:{attempt.Id}"));
            progressSignals.RecordActivity(context.UserId, "translation-attempt", attempt.Id, attempt.SubmittedAt);
            return Results.Created($"/api/translation/attempts/{attempt.Id}", ToAttemptView(attempt));
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

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(5));

            TranslationFeedback? feedback;
            try
            {
                feedback = await feedbackGateway.RequestAsync(attempt, timeoutSource.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                return Results.Problem(
                    "Dịch vụ góp ý phản hồi quá lâu. Câu trả lời của bạn vẫn được lưu.",
                    statusCode: StatusCodes.Status504GatewayTimeout,
                    title: "Feedback timeout");
            }
            catch
            {
                return Results.Problem(
                    "Dịch vụ góp ý đang tạm thời không khả dụng. Câu trả lời của bạn vẫn được lưu.",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Feedback provider failure");
            }

            if (feedback is null)
            {
                return Results.Problem("AI feedback provider chưa được cấu hình.", statusCode: StatusCodes.Status503ServiceUnavailable, title: "Feedback unavailable");
            }

            var feedbackValidationError = TranslationValidation.ValidateFeedback(feedback);
            if (feedbackValidationError is not null)
            {
                return Results.Problem(
                    feedbackValidationError,
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Invalid feedback response");
            }

            var updated = store.SetFeedback(context.UserId, id, feedback);
            return updated is null ? Results.NotFound() : Results.Ok(ToAttemptView(updated));
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
