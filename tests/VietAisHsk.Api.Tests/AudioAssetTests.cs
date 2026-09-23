using VietAisHsk.Api.Modules.Content;

namespace VietAisHsk.Api.Tests;

public sealed class AudioAssetTests
{
    [Fact]
    public void Audio_request_is_idempotent_and_enqueues_one_message()
    {
        var queue = new InMemoryAudioGenerationQueue();
        var store = new InMemoryAudioAssetStore(queue);
        var request = new RequestAudioGenerationRequest(
            "lesson-grammar-context",
            "把字句用于强调受事。",
            "cosyvoice-v1",
            "lesson-grammar-context:audio:v1");

        var first = store.Request(request);
        var duplicate = store.Request(request);

        Assert.True(first.Created);
        Assert.False(duplicate.Created);
        Assert.False(duplicate.Conflict);
        Assert.Equal(first.Asset.Id, duplicate.Asset.Id);
        Assert.Equal(AudioAssetStatus.Pending, first.Asset.Status);
        Assert.Single(queue.Messages);
    }

    [Fact]
    public void Reusing_idempotency_key_for_different_audio_is_rejected()
    {
        var store = new InMemoryAudioAssetStore();
        var key = "same-key";

        _ = store.Request(new RequestAudioGenerationRequest("content-a", "你好。", "voice-a", key));
        var conflict = store.Request(new RequestAudioGenerationRequest("content-b", "再见。", "voice-b", key));

        Assert.True(conflict.Conflict);
        Assert.False(conflict.Created);
    }

    [Fact]
    public void Failed_audio_retry_increments_attempt_and_enqueues_new_key()
    {
        var queue = new InMemoryAudioGenerationQueue();
        var store = new InMemoryAudioAssetStore(queue);

        var failed = store.Get("audio-video-pronunciation");
        Assert.NotNull(failed);
        var retry = store.Retry(failed!.Id);
        Assert.NotNull(retry);

        Assert.True(retry.Retried);
        Assert.False(retry.Conflict);
        Assert.Equal(AudioAssetStatus.Pending, retry.Asset.Status);
        Assert.Equal(failed.AttemptCount + 1, retry.Asset.AttemptCount);
        Assert.Contains(queue.Messages, message => message.IdempotencyKey == retry.Asset.IdempotencyKey);

        var secondRetry = store.Retry(failed.Id);
        Assert.NotNull(secondRetry);
        Assert.False(secondRetry.Retried);
        Assert.True(secondRetry.Conflict);
        Assert.Single(queue.Messages);
    }

    [Fact]
    public void Audio_worker_state_transitions_keep_ready_asset_playable()
    {
        var store = new InMemoryAudioAssetStore();
        var requested = store.Request(new RequestAudioGenerationRequest(
            "lesson-speaking",
            "你好，我叫小明。",
            "cosyvoice-v1",
            "lesson-speaking:audio:v1"));

        Assert.NotNull(store.MarkProcessing(requested.Asset.Id));
        Assert.NotNull(store.MarkReady(requested.Asset.Id, "/assets/audio/lesson-speaking.mp3"));

        var ready = store.GetReady(requested.Asset.Id);
        Assert.NotNull(ready);
        Assert.Equal(AudioAssetStatus.Ready, ready.Status);
        Assert.Equal("/assets/audio/lesson-speaking.mp3", ready.AudioUrl);
    }
}
