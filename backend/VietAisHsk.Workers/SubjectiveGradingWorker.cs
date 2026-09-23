using System.Net;
using System.Text;
using System.Text.Json;
using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Exam;

namespace VietAisHsk.Workers;

public sealed record SubjectiveGradingProviderResult(
    string Status,
    int? Score,
    string? Feedback,
    string SchemaVersion);

public interface ISubjectiveGradingProvider
{
    ValueTask<SubjectiveGradingProviderResult> GradeAsync(
        SubjectiveGradingRequestedMessage message,
        CancellationToken cancellationToken);
}

/// <summary>
/// Fail-closed adapter used until a reviewed AI provider is selected and configured.
/// The worker still acknowledges the queue job through the signed API callback, preserving
/// the failed state and avoiding an unbounded retry loop that pretends an AI provider exists.
/// </summary>
public sealed class UnconfiguredSubjectiveGradingProvider : ISubjectiveGradingProvider
{
    public ValueTask<SubjectiveGradingProviderResult> GradeAsync(
        SubjectiveGradingRequestedMessage message,
        CancellationToken cancellationToken) => ValueTask.FromResult(
            new SubjectiveGradingProviderResult(
                "Failed",
                null,
                "AI grading provider chưa được cấu hình; bài làm đã được lưu để xử lý lại sau.",
                message.SchemaVersion));
}

public sealed class SubjectiveGradingHandler(
    ISubjectiveGradingProvider provider,
    WorkerCallbackClient callbackClient,
    ILogger<SubjectiveGradingHandler> logger)
{
    public async Task Handle(SubjectiveGradingRequestedMessage message, CancellationToken cancellationToken)
    {
        Validate(message);
        var result = await provider.GradeAsync(message, cancellationToken);
        await callbackClient.ApplyAsync(message, result, cancellationToken);
        logger.LogInformation(
            "Subjective grading job {JobId} finished with {Status} for attempt {AttemptId}",
            message.JobId,
            result.Status,
            message.AttemptId);
    }

    private static void Validate(SubjectiveGradingRequestedMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.JobId)
            || string.IsNullOrWhiteSpace(message.UserId)
            || string.IsNullOrWhiteSpace(message.AttemptId)
            || string.IsNullOrWhiteSpace(message.SchemaVersion))
        {
            throw new InvalidOperationException("Subjective grading message thiếu correlation hoặc schema metadata.");
        }
    }
}

public sealed class WorkerCallbackClient(
    HttpClient httpClient,
    IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string sharedSecret = configuration["Workers:Callback:SharedSecret"]?.Trim()
        ?? throw new InvalidOperationException("Worker callback secret chưa được cấu hình.");

    public async Task ApplyAsync(
        SubjectiveGradingRequestedMessage message,
        SubjectiveGradingProviderResult result,
        CancellationToken cancellationToken)
    {
        var payload = new ApplySubjectiveGradingResultRequest(
            message.JobId,
            message.AttemptId,
            result.Status,
            result.Score,
            result.Feedback,
            result.SchemaVersion,
            message.UserId);
        var body = JsonSerializer.Serialize(payload, JsonOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/exam-attempts/{Uri.EscapeDataString(message.AttemptId)}/subjective-grading")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-VietAIS-Worker-Timestamp", timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-VietAIS-Worker-Signature", HmacWorkerCallbackAuthenticator.Sign(sharedSecret, timestamp, Encoding.UTF8.GetBytes(body)));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Worker callback failed with {(int)response.StatusCode} {response.StatusCode}: {responseBody}",
            null,
            response.StatusCode);
    }
}
