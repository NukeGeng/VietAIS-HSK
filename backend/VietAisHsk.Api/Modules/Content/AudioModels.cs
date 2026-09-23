namespace VietAisHsk.Api.Modules.Content;

public enum AudioAssetStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
}

public sealed record AudioAsset(
    string Id,
    string ContentId,
    string Text,
    string Voice,
    AudioAssetStatus Status,
    int AttemptCount,
    string IdempotencyKey,
    string? AudioUrl,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RequestAudioGenerationRequest(
    string? ContentId,
    string? Text,
    string? Voice,
    string? IdempotencyKey);

public sealed record AudioGenerationRequested(
    string AudioAssetId,
    string ContentId,
    string Text,
    string Voice,
    int AttemptCount,
    string IdempotencyKey,
    DateTimeOffset RequestedAt);

public sealed record AudioRequestOutcome(AudioAsset Asset, bool Created, bool Conflict);

public sealed record AudioRetryOutcome(AudioAsset Asset, bool Retried, bool Conflict);

public interface IAudioGenerationQueue
{
    IReadOnlyList<AudioGenerationRequested> Messages { get; }
    bool Enqueue(AudioGenerationRequested message);
}

public interface IAudioAssetStore
{
    IReadOnlyList<AudioAsset> GetAll();
    AudioAsset? Get(string id);
    AudioAsset? GetReady(string id);
    AudioRequestOutcome Request(RequestAudioGenerationRequest request);
    AudioRetryOutcome? Retry(string id);
    AudioAsset? MarkProcessing(string id);
    AudioAsset? MarkReady(string id, string audioUrl);
    AudioAsset? MarkFailed(string id, string error);
}
