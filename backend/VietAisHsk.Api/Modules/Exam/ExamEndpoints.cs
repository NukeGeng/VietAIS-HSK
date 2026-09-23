using System.Text.Json;
using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Review;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Exam;

public static class ExamEndpoints
{
    public static IEndpointRouteBuilder MapExamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var catalog = endpoints.MapGroup("/api");

        catalog.MapGet("/exams", (IExamCatalog exams) => Results.Ok(exams.GetPublishedExams().Select(ToDefinitionView)));

        catalog.MapGet("/exams/{examId}", (string examId, IExamCatalog exams) =>
        {
            var exam = exams.GetPublishedExam(examId);
            return exam is null ? Results.NotFound() : Results.Ok(ToDefinitionView(exam));
        });

        catalog.MapPost("/exams/{examId}/attempts", (string examId, IUserContextAccessor contextAccessor, IExamCatalog exams, IExamStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var exam = exams.GetPublishedExam(examId);
            return exam is null
                ? Results.NotFound()
                : Results.Created($"/api/exam-attempts/{{id}}", ToAttemptView(store.Start(context.UserId, exam)));
        });

        catalog.MapGet("/exam-attempts/{id}", (string id, IUserContextAccessor contextAccessor, IExamStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var attempt = store.Get(context.UserId, id);
            return attempt is null ? Results.NotFound() : Results.Ok(ToAttemptView(attempt));
        });

        catalog.MapPost("/exam-attempts/{id}/answers", (string id, ExamAnswerRequest request, IUserContextAccessor contextAccessor, IExamStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var current = store.Get(context.UserId, id);
            if (current is null)
            {
                return Results.NotFound();
            }

            if (current.Status != ExamAttemptStatus.Active)
            {
                return Results.Problem("Bài thi đã được nộp.", statusCode: StatusCodes.Status409Conflict, title: "Business rule");
            }

            var updated = store.SubmitAnswer(context.UserId, id, request);
            return updated is null ? Results.NotFound() : Results.Ok(ToAttemptView(updated));
        });

        catalog.MapPost("/exam-attempts/{id}/submit", (string id, IUserContextAccessor contextAccessor, IExamStore store, IProgressSignalSink progressSignals, IReviewSignalSink reviewSignals) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var submitted = store.Submit(context.UserId, id);
            if (submitted is null)
            {
                return Results.NotFound();
            }

            var result = store.GetResult(context.UserId, id);
            if (result is not null && submitted.ObjectiveScore.HasValue)
            {
                var evaluatedAt = submitted.Events
                    .LastOrDefault(item => item.Type == "ObjectiveScoreCalculated" || item.Type == "ExamSubmitted")?.OccurredAt
                    ?? DateTimeOffset.UtcNow;
                var examSignal = new ExamResultSignal(
                    context.UserId,
                    submitted.ExamId,
                    submitted.Id,
                    result.Correct,
                    result.Total,
                    submitted.ObjectiveScore.Value,
                    result.IncorrectQuestionIds,
                    evaluatedAt,
                    $"exam:{submitted.Id}",
                    submitted.Questions
                        .Where(question => !string.IsNullOrWhiteSpace(question.KnowledgeType) && !string.IsNullOrWhiteSpace(question.KnowledgeId))
                        .Select(question => new ExamKnowledgeResult(
                            question.Id,
                            question.KnowledgeType!,
                            question.KnowledgeId!,
                            result.IncorrectQuestionIds.Contains(question.Id, StringComparer.OrdinalIgnoreCase) ? "Incorrect" : "Correct"))
                        .ToArray());
                progressSignals.ApplyExamSignal(examSignal);
                reviewSignals.ApplyExamSignal(examSignal);
                progressSignals.RecordActivity(context.UserId, "exam-submitted", submitted.Id, evaluatedAt);
            }

            return Results.Ok(ToAttemptView(submitted));
        });

        catalog.MapGet("/exam-attempts/{id}/result", (string id, IUserContextAccessor contextAccessor, IExamStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var result = store.GetResult(context.UserId, id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // Local adapter seam for the future AI worker. Production should call this
        // through an authenticated worker transport, not expose it as learner UI.
        catalog.MapPost("/exam-attempts/{id}/subjective-grading", async (
            string id,
            HttpRequest httpRequest,
            IUserContextAccessor contextAccessor,
            IHostEnvironment hostEnvironment,
            IWorkerCallbackAuthenticator workerCallbackAuthenticator,
            IExamStore store) =>
        {
            var context = contextAccessor.Current;
            ApplySubjectiveGradingResultRequest? request;
            string userId;

            if (workerCallbackAuthenticator.IsConfigured)
            {
                using var body = new MemoryStream();
                await httpRequest.Body.CopyToAsync(body);
                var payload = body.ToArray();
                if (!workerCallbackAuthenticator.TryVerify(httpRequest, payload, out _))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    request = JsonSerializer.Deserialize<ApplySubjectiveGradingResultRequest>(payload, JsonSerializerOptions.Web);
                }
                catch (JsonException)
                {
                    request = null;
                }
                if (request is null || string.IsNullOrWhiteSpace(request.UserId))
                {
                    return Results.Problem(
                        "Worker callback phải chứa UserId trong payload đã ký.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid worker callback");
                }

                userId = request.UserId.Trim();
            }
            else
            {
                if (context is null)
                {
                    return Results.Unauthorized();
                }

                if (!hostEnvironment.IsDevelopment() || !context.Permissions.Contains("exam.subjective-grading"))
                {
                    return Results.Forbid();
                }

                try
                {
                    request = await httpRequest.ReadFromJsonAsync<ApplySubjectiveGradingResultRequest>(JsonSerializerOptions.Web);
                }
                catch (JsonException)
                {
                    request = null;
                }
                if (request is null)
                {
                    return Results.Problem(
                        "Payload chấm tự luận không hợp lệ.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid worker callback");
                }

                userId = context.UserId;
            }

            var outcome = store.ApplySubjectiveGradingResult(
                userId,
                request with { AttemptId = id });
            if (outcome is null)
            {
                return Results.NotFound();
            }

            return outcome.Conflict
                ? Results.Problem("Kết quả chấm không khớp job đang chờ.", statusCode: StatusCodes.Status409Conflict, title: "Subjective grading conflict")
                : Results.Ok(ToAttemptView(outcome.Attempt));
        });

        return endpoints;
    }

    private static ExamDefinitionView ToDefinitionView(ExamDefinition exam) =>
        new(exam.Id, exam.Name, exam.HskLevel, exam.ContentVersion, exam.Questions.Select(ToQuestionView).ToArray());

    private static ExamAttemptView ToAttemptView(ExamAttempt attempt) =>
        new(attempt.Id, attempt.ExamId, attempt.ContentVersion, attempt.Status, attempt.Questions.Select(ToQuestionView).ToArray(), attempt.Answers, attempt.ObjectiveScore, attempt.SubjectiveGradingStatus, attempt.SubjectiveScore, attempt.SubjectiveFeedback);

    private static ExamQuestionView ToQuestionView(ExamQuestion question) => new(question.Id, question.Prompt, question.QuestionType);
}
