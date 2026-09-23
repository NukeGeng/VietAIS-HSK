# Speaking Module

## 1. Mục đích

Cung cấp luyện nói và đối thoại tiếng Trung theo level/context, gồm speech recognition/assessment, AI dialogue và summary.

## 2. Phạm vi

Modes có thể gồm:
- Nghe và lặp lại;
- Đọc thành tiếng;
- Hội thoại có hướng dẫn;
- Tình huống;
- Tự do.

Không biến Speaking thành chatbot chung ngoài học tiếng Trung.

## 3. Flow hội thoại

```text
Start session
→ AI/topic prompt
→ user audio
→ STT / pronunciation assessment
→ transcript + score metadata
→ DeepSeek tạo response phù hợp level
→ TTS response
→ next turn
→ end session
→ summary
```

Per-turn realtime không đi RabbitMQ.

## 4. Phân vai hệ thống

Speech service:
- transcript;
- pronunciation/fluency/prosody nếu provider hỗ trợ.

DeepSeek:
- nội dung;
- relevance;
- grammar;
- vocabulary;
- naturalness;
- dialogue continuation.

Không dùng LLM để tự suy đoán acoustic pronunciation từ text transcript.

## 5. Level guard

Session có HSK/context.

Prompt ưu tiên vocabulary/grammar phù hợp level; nếu dùng từ vượt level phải có policy giải thích/giới hạn.

## 6. Data model

```text
SpeakingSession
SpeakingTurn
SpeakingAssessmentSummary
```

Raw audio persistence không phải requirement bắt buộc; retention/storage policy là decision riêng.

## 7. Events/messages

`SpeakingSessionCompleted` làm input Progress.

Post-session analysis có thể async nếu UX không cần ngay; per-turn không dùng broker.

## 8. Commands / Queries

- StartSpeakingSession;
- SubmitSpeakingTurn / realtime endpoint contract;
- EndSpeakingSession;
- GetSpeakingSessionSummary;
- GetSpeakingHistory.

## 9. API/Realtime

Exact WebSocket/WebRTC/HTTP streaming decision để PoC xác nhận.

Không khóa transport trước benchmark.

## 10. AI

Có — đúng use case đã duyệt.

Cần usage/cost logging và session limits theo product policy.

## 11. Test cases

- [x] start/end session; endpoint end phát `SpeakingSessionCompletedSignal` và retry end không tăng metric;
- [ ] STT error handling;
- [x] AI timeout handling;
- [ ] TTS failure handling;
- [x] level context preserved; signal giữ `HskContext` của session;
- [x] own-session permission;
- [x] no RabbitMQ per-turn; provider được gọi trực tiếp qua seam, không có broker trong flow;
- [ ] Computer Use/browser microphone flow khi environment cho phép.
