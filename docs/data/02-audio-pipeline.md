# Audio Pipeline

## Vocabulary / sentence audio

```text
Text + approved pronunciation/Pinyin
→ AudioGenerationRequested
→ Wolverine Outbox
→ RabbitMQ
→ CosyVoice Worker
→ generated audio
→ Object Storage
→ AudioAsset Ready
→ CDN/served URL
```

## Rule

- generate một lần, tái sử dụng nhiều lần;
- learner bấm nghe không gọi CosyVoice;
- lưu voice/model/config version;
- với đa âm/polyphonic word phải hỗ trợ pronunciation override đã duyệt;
- audio job idempotent;
- regeneration tạo version/update có kiểm soát, không overwrite im lặng.

## Scope

Audio có thể dùng cho:
- vocabulary;
- example sentence;
- beginner Pinyin/tone samples;
- story/video supplemental audio khi phù hợp.
