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
            return Results.Created($"/api/practice/sessions/{session.Id}", session);
        });

        group.MapGet("/sessions/{id}", (string id, IUserContextAccessor contextAccessor, IPracticeStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var session = store.Get(context.UserId, id);
            return session is null ? Results.NotFound() : Results.Ok(session);
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
            reviewSignals.ApplyPracticeSignal(new PracticeEvaluationSignal(
                context.UserId,
                question.Type,
                question.Id,
                attempt.Result.ToString(),
                attempt.SubmittedAt));
            progressSignals.ApplyPracticeSignal(new PracticeEvaluationSignal(
                context.UserId,
                question.Type,
                question.Id,
                attempt.Result.ToString(),
                attempt.SubmittedAt));
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

        return endpoints;
    }
}
