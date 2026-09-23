using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Speaking;

public static class SpeakingEndpoints
{
    public static IEndpointRouteBuilder MapSpeakingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/speaking");

        group.MapPost("/sessions", (StartSpeakingSessionRequest request, IUserContextAccessor contextAccessor, ISpeakingStore store) =>
        {
            if (contextAccessor.Current is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.HskContext) || string.IsNullOrWhiteSpace(request.Mode))
            {
                return Results.Problem("HskContext và Mode là bắt buộc.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var session = store.Create(contextAccessor.Current.UserId, request);
            return Results.Created($"/api/speaking/sessions/{session.Id}", session);
        });

        group.MapGet("/sessions/{id}", (string id, IUserContextAccessor contextAccessor, ISpeakingStore store) =>
        {
            if (contextAccessor.Current is null)
            {
                return Results.Unauthorized();
            }

            var session = store.Get(contextAccessor.Current.UserId, id);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        group.MapPost("/sessions/{id}/turns", async (string id, SubmitSpeakingTurnRequest request, IUserContextAccessor contextAccessor, ISpeakingStore store, ISpeakingProvider provider, CancellationToken cancellationToken) =>
        {
            if (contextAccessor.Current is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Transcript))
            {
                return Results.Problem("Transcript là bắt buộc ở bootstrap HTTP flow.", statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var session = store.Get(contextAccessor.Current.UserId, id);
            if (session is null)
            {
                return Results.NotFound();
            }

            if (session.Status == SpeakingSessionStatus.Ended)
            {
                return Results.Problem("Speaking session đã kết thúc.", statusCode: StatusCodes.Status409Conflict, title: "Business rule");
            }

            var provisional = new SpeakingTurn("pending", request.Transcript.Trim(), request.AudioReference?.Trim(), "pending", null, DateTimeOffset.UtcNow);
            string? response;
            string providerStatus;
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                response = await provider.GenerateResponseAsync(session, provisional, timeoutSource.Token);
                providerStatus = response is null ? "unavailable" : "completed";
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                response = null;
                providerStatus = "timeout";
            }
            catch
            {
                response = null;
                providerStatus = "error";
            }

            var updated = store.AddTurn(contextAccessor.Current.UserId, id, request, providerStatus, response);
            return updated is null
                ? Results.NotFound()
                : Results.Ok(new SpeakingTurnResult(updated.Turns[^1], response is null ? "retry-provider" : "continue"));
        });

        group.MapPost("/sessions/{id}/end", (string id, IUserContextAccessor contextAccessor, ISpeakingStore store, IProgressSignalSink progressSignals) =>
        {
            if (contextAccessor.Current is null)
            {
                return Results.Unauthorized();
            }

            var session = store.End(contextAccessor.Current.UserId, id);
            if (session is null)
            {
                return Results.NotFound();
            }

            var completedAt = session.EndedAt ?? DateTimeOffset.UtcNow;
            progressSignals.ApplySpeakingSignal(new SpeakingSessionCompletedSignal(
                contextAccessor.Current.UserId,
                session.Id,
                session.HskContext,
                session.Turns.Count,
                session.StartedAt,
                completedAt,
                $"speaking:{session.Id}"));
            progressSignals.RecordActivity(contextAccessor.Current.UserId, "speaking-session-completed", session.Id, completedAt);
            return Results.Ok(session);
        });

        group.MapGet("/history", (IUserContextAccessor contextAccessor, ISpeakingStore store) =>
        {
            return contextAccessor.Current is null
                ? Results.Unauthorized()
                : Results.Ok(store.History(contextAccessor.Current.UserId));
        });

        return endpoints;
    }
}
