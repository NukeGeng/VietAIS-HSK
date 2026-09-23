namespace VietAisHsk.Api.Modules.Content;

public sealed class InMemoryAudioGenerationQueue : IAudioGenerationQueue
{
    private readonly object gate = new();
    private readonly List<AudioGenerationRequested> messages = [];

    public IReadOnlyList<AudioGenerationRequested> Messages
    {
        get
        {
            lock (gate)
            {
                return messages.ToArray();
            }
        }
    }

    public bool Enqueue(AudioGenerationRequested message)
    {
        lock (gate)
        {
            if (messages.Any(existing => existing.IdempotencyKey.Equals(message.IdempotencyKey, StringComparison.Ordinal)))
            {
                return false;
            }

            messages.Add(message);
            return true;
        }
    }
}

/// <summary>
/// Deterministic local seam for the Content audio workflow.
/// A production adapter can replace the queue with Wolverine/RabbitMQ without changing the API contract.
/// </summary>
public sealed class InMemoryAudioAssetStore : IAudioAssetStore
{
    private const string DefaultVoice = "cosyvoice-v1";
    private readonly object gate = new();
    private readonly IAudioGenerationQueue queue;
    private readonly Dictionary<string, AudioAsset> assets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["audio-story-classroom"] = new(
            "audio-story-classroom",
            "story-classroom",
            "今天我在教室学习汉语。",
            DefaultVoice,
            AudioAssetStatus.Ready,
            1,
            "audio-story-classroom:attempt:1",
            "/assets/audio/story-classroom.mp3",
            null,
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 8, 2, 0, TimeSpan.Zero)),
        ["audio-video-pronunciation"] = new(
            "audio-video-pronunciation",
            "video-pronunciation-draft",
            "第三声先降后升。",
            DefaultVoice,
            AudioAssetStatus.Failed,
            1,
            "audio-video-pronunciation:attempt:1",
            null,
            "Audio provider timeout; cần thử lại.",
            new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 2, 8, 5, 0, TimeSpan.Zero)),
    };

    private int nextId;

    public InMemoryAudioAssetStore()
        : this(new InMemoryAudioGenerationQueue())
    {
    }

    public InMemoryAudioAssetStore(IAudioGenerationQueue queue)
    {
        this.queue = queue;
    }

    public IReadOnlyList<AudioAsset> GetAll()
    {
        lock (gate)
        {
            return assets.Values
                .OrderByDescending(asset => asset.UpdatedAt)
                .ThenBy(asset => asset.Id, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public AudioAsset? Get(string id)
    {
        lock (gate)
        {
            return assets.TryGetValue(id.Trim(), out var asset) ? asset : null;
        }
    }

    public AudioAsset? GetReady(string id)
    {
        lock (gate)
        {
            return assets.TryGetValue(id.Trim(), out var asset) && asset.Status == AudioAssetStatus.Ready
                ? asset
                : null;
        }
    }

    public AudioRequestOutcome Request(RequestAudioGenerationRequest request)
    {
        var contentId = request.ContentId!.Trim();
        var text = request.Text!.Trim();
        var voice = string.IsNullOrWhiteSpace(request.Voice) ? DefaultVoice : request.Voice.Trim();
        var idempotencyKey = request.IdempotencyKey!.Trim();

        lock (gate)
        {
            var existing = assets.Values.FirstOrDefault(asset => asset.IdempotencyKey.Equals(idempotencyKey, StringComparison.Ordinal));
            if (existing is not null)
            {
                var sameRequest = existing.ContentId.Equals(contentId, StringComparison.Ordinal)
                    && existing.Text.Equals(text, StringComparison.Ordinal)
                    && existing.Voice.Equals(voice, StringComparison.Ordinal);
                return new AudioRequestOutcome(existing, false, !sameRequest);
            }

            var now = DateTimeOffset.UtcNow;
            var asset = new AudioAsset(
                $"audio-local-{Interlocked.Increment(ref nextId):D4}",
                contentId,
                text,
                voice,
                AudioAssetStatus.Pending,
                0,
                idempotencyKey,
                null,
                null,
                now,
                now);
            assets[asset.Id] = asset;
            queue.Enqueue(new AudioGenerationRequested(
                asset.Id,
                asset.ContentId,
                asset.Text,
                asset.Voice,
                asset.AttemptCount,
                asset.IdempotencyKey,
                now));
            return new AudioRequestOutcome(asset, true, false);
        }
    }

    public AudioRetryOutcome? Retry(string id)
    {
        lock (gate)
        {
            if (!assets.TryGetValue(id.Trim(), out var current))
            {
                return null;
            }

            if (current.Status != AudioAssetStatus.Failed)
            {
                return new AudioRetryOutcome(current, false, true);
            }

            var now = DateTimeOffset.UtcNow;
            var attempt = current.AttemptCount + 1;
            var retried = current with
            {
                Status = AudioAssetStatus.Pending,
                AttemptCount = attempt,
                IdempotencyKey = $"{current.Id}:attempt:{attempt}",
                AudioUrl = null,
                LastError = null,
                UpdatedAt = now,
            };
            assets[retried.Id] = retried;
            queue.Enqueue(new AudioGenerationRequested(
                retried.Id,
                retried.ContentId,
                retried.Text,
                retried.Voice,
                retried.AttemptCount,
                retried.IdempotencyKey,
                now));
            return new AudioRetryOutcome(retried, true, false);
        }
    }

    public AudioAsset? MarkProcessing(string id)
    {
        lock (gate)
        {
            if (!assets.TryGetValue(id.Trim(), out var current)) return null;
            if (current.Status == AudioAssetStatus.Processing) return current;
            if (current.Status != AudioAssetStatus.Pending) return null;
            var updated = current with { Status = AudioAssetStatus.Processing, UpdatedAt = DateTimeOffset.UtcNow };
            assets[updated.Id] = updated;
            return updated;
        }
    }

    public AudioAsset? MarkReady(string id, string audioUrl)
    {
        lock (gate)
        {
            if (!assets.TryGetValue(id.Trim(), out var current) || string.IsNullOrWhiteSpace(audioUrl)) return null;
            var updated = current with
            {
                Status = AudioAssetStatus.Ready,
                AudioUrl = audioUrl.Trim(),
                LastError = null,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            assets[updated.Id] = updated;
            return updated;
        }
    }

    public AudioAsset? MarkFailed(string id, string error)
    {
        lock (gate)
        {
            if (!assets.TryGetValue(id.Trim(), out var current) || string.IsNullOrWhiteSpace(error)) return null;
            var updated = current with
            {
                Status = AudioAssetStatus.Failed,
                LastError = error.Trim(),
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            assets[updated.Id] = updated;
            return updated;
        }
    }
}
