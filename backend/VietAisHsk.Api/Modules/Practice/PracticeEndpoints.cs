using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Review;
using VietAisHsk.Api.Modules.Shared;
using VietAisHsk.Api.Modules.Progress;

namespace VietAisHsk.Api.Modules.Practice;

public static class PracticeEndpoints
{
    public static IEndpointRouteBuilder MapPracticeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/practice");

        group.MapGet("/questions", (string? type, IUserContextAccessor contextAccessor, IPracticeQuestionReader reader) =>
        {
            if (contextAccessor.Current is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(reader.GetPublishedQuestions(type).Select(ToQuestionView));
        });

        group.MapPost("/sessions", (StartPracticeSessionRequest request, IUserContextAccessor contextAccessor, IPracticeQuestionReader reader, IPracticeStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            if (request.QuestionIds is null || request.QuestionIds.Count == 0)
            {
                return Results.Problem("QuestionIds là bắt buộc.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var questions = reader.GetPublishedQuestions(request.QuestionIds);
            if (questions.Count != request.QuestionIds.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                return Results.Problem("Một hoặc nhiều câu hỏi không tồn tại hoặc chưa Published.", statusCode: StatusCodes.Status422UnprocessableEntity, title: "Question unavailable");
            }

            var session = store.Create(context.UserId, questions)!;
            return Results.Created($"/api/practice/sessions/{session.Id}", ToSessionView(session));
        });

        group.MapGet("/sessions/{id}", (string id, IUserContextAccessor contextAccessor, IPracticeStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var session = store.Get(context.UserId, id);
            return session is null ? Results.NotFound() : Results.Ok(ToSessionView(session));
        });

        group.MapPost("/sessions/{id}/answers", (string id, SubmitPracticeAnswerRequest request, IUserContextAccessor contextAccessor, IPracticeStore store, IReviewSignalSink reviewSignals, IProgressSignalSink progressSignals) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.QuestionId) || request.Answer is null)
            {
                return Results.Problem("QuestionId và Answer là bắt buộc.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var session = store.Get(context.UserId, id);
            if (session is null)
            {
                return Results.NotFound();
            }

            if (session.Status == PracticeSessionStatus.Completed)
            {
                return Results.Problem("Practice session đã hoàn thành.", statusCode: StatusCodes.Status409Conflict, title: "Business rule");
            }

            var attempt = store.SubmitAnswer(context.UserId, id, request.QuestionId.Trim(), request.Answer);
            if (attempt is null)
            {
                return Results.NotFound();
            }

            var question = session.Questions.First(item => string.Equals(item.Id, attempt.QuestionId, StringComparison.OrdinalIgnoreCase));
            var signal = new PracticeEvaluationSignal(
                context.UserId,
                question.Type,
                question.Id,
                attempt.Result.ToString(),
                attempt.SubmittedAt,
                $"practice:{id}:{question.Id}");
            reviewSignals.ApplyPracticeSignal(signal);
            progressSignals.ApplyPracticeSignal(signal);
            return Results.Ok(attempt);
        });

        group.MapPost("/sessions/{id}/complete", (string id, IUserContextAccessor contextAccessor, IPracticeStore store, IProgressSignalSink progressSignals) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var session = store.Complete(context.UserId, id);
            if (session is null)
            {
                return Results.NotFound();
            }

            progressSignals.RecordActivity(context.UserId, "practice-completed", session.Id, session.UpdatedAt);
            return Results.Ok(store.GetResult(session));
        });

        group.MapPost("/hanzi/{hanziId}/attempts", (string hanziId, StartHanziWritingAttemptRequest request, IUserContextAccessor contextAccessor, IHanziWritingStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var mode = string.IsNullOrWhiteSpace(request.Mode)
                ? HanziWritingMode.Guided
                : Enum.TryParse<HanziWritingMode>(request.Mode, true, out var parsedMode)
                    ? parsedMode
                    : (HanziWritingMode?)null;
            if (mode is null)
            {
                return Results.Problem("Mode phải là Guided, Trace hoặc Recall.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var attempt = store.Start(context.UserId, hanziId.Trim(), mode.Value);
            return attempt is null
                ? Results.NotFound()
                : Results.Created($"/api/practice/hanzi/attempts/{attempt.Id}", HanziWritingAttemptMapper.ToView(attempt));
        });

        group.MapGet("/hanzi/attempts/{id}", (string id, IUserContextAccessor contextAccessor, IHanziWritingStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var attempt = store.Get(context.UserId, id);
            return attempt is null ? Results.NotFound() : Results.Ok(HanziWritingAttemptMapper.ToView(attempt));
        });

        group.MapPost("/hanzi/attempts/{id}/strokes", (string id, SubmitHanziStrokeRequest request, IUserContextAccessor contextAccessor, IHanziWritingStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            if (request.Points is null || request.Points.Count < 2)
            {
                return Results.Problem("Points phải có ít nhất hai điểm.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var submission = store.SubmitStroke(context.UserId, id, request.Points);
            return submission is null
                ? Results.NotFound()
                : Results.Ok(new
                {
                    attempt = HanziWritingAttemptMapper.ToView(submission.Attempt),
                    result = submission.Result
                });
        });

        group.MapPost("/hanzi/attempts/{id}/complete", (string id, IUserContextAccessor contextAccessor, IHanziWritingStore store, IReviewSignalSink reviewSignals, IProgressSignalSink progressSignals) =>
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

            if (current.CompletedAt is not null)
            {
                return Results.Ok(HanziWritingAttemptMapper.ToView(current));
            }

            var completed = store.Complete(context.UserId, id);
            if (completed is null)
            {
                return Results.NotFound();
            }

            var result = completed.OverallResult == PracticeResult.Correct ? "Correct" : "Incorrect";
            var completedAt = completed.CompletedAt ?? DateTimeOffset.UtcNow;
            var signal = new PracticeEvaluationSignal(
                context.UserId,
                "hanzi-writing",
                completed.HanziId,
                result,
                completedAt,
                $"hanzi-writing:{completed.Id}");
            reviewSignals.ApplyPracticeSignal(signal);
            progressSignals.ApplyPracticeSignal(signal);
            progressSignals.RecordActivity(context.UserId, "hanzi-writing-completed", completed.Id, completedAt);
            return Results.Ok(HanziWritingAttemptMapper.ToView(completed));
        });

        return endpoints;
    }

    private static PracticeQuestionView ToQuestionView(PracticeQuestion question) =>
        new(question.Id, question.Type, question.Prompt, question.Status, question.Options, question.ContentVersion);

    private static PracticeSessionView ToSessionView(PracticeSession session) =>
        new(session.Id, session.Status, session.Questions.Select(ToQuestionView).ToArray(), session.Attempts, session.CreatedAt, session.UpdatedAt);
}
