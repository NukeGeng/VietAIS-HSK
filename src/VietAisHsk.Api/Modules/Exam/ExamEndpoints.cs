using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Modules.Exam;

public static class ExamEndpoints
{
    public static IEndpointRouteBuilder MapExamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var catalog = endpoints.MapGroup("/api");

        catalog.MapGet("/exams", (IExamCatalog exams) => Results.Ok(exams.GetPublishedExams()));

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
                : Results.Created($"/api/exam-attempts/{{id}}", store.Start(context.UserId, exam));
        });

        catalog.MapGet("/exam-attempts/{id}", (string id, IUserContextAccessor contextAccessor, IExamStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var attempt = store.Get(context.UserId, id);
            return attempt is null ? Results.NotFound() : Results.Ok(attempt);
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
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        catalog.MapPost("/exam-attempts/{id}/submit", (string id, IUserContextAccessor contextAccessor, IExamStore store) =>
        {
            var context = contextAccessor.Current;
            if (context is null)
            {
                return Results.Unauthorized();
            }

            var submitted = store.Submit(context.UserId, id);
            return submitted is null ? Results.NotFound() : Results.Ok(submitted);
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

        return endpoints;
    }
}
