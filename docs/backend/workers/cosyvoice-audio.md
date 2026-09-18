# CosyVoice Audio Worker

## Mục đích

Sinh audio Mandarin trước và lưu thành asset tái sử dụng.

## Input message

Ví dụ:

```text
AudioGenerationRequested
- JobId
- EntityType
- EntityId
- Text
- Pinyin/pronunciation override khi cần
- VoiceProfile
- OutputFormat
```

## Flow

```text
RabbitMQ
→ Worker
→ validate job/idempotency
→ CosyVoice
→ output WAV/PCM
→ encode MP3/Opus nếu cần
→ Storage
→ AudioGenerated
```

## Rules

- không generate realtime khi learner bấm audio;
- job phải idempotent;
- polyphonic/known pronunciation phải dùng approved Pinyin override strategy khi cần;
- failed job ghi error state và retry giới hạn;
- không overwrite ready asset im lặng nếu config/voice/version khác;
- lưu model/voice/version metadata đủ để regenerate sau này.
