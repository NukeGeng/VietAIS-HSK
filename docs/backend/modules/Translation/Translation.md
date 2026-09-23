# Translation Module

## 1. Mục đích

Luyện Dịch Việt - Trung; backend cung cấp câu, lưu attempt và chỉ gọi AI khi learner yêu cầu góp ý cho câu mở.

## 2. Phạm vi

- translation exercise catalog reference;
- learner answer;
- reference answer;
- optional AI feedback;
- attempt history;
- result signal cho Progress khi phù hợp.

## 3. Actor & phân quyền

Learner chỉ xem attempt của mình; Admin quản lý source exercise qua Content/Curriculum contract tùy loại nội dung.

## 4. Flow

```text
Load câu Việt
→ learner nhập Trung
→ lưu attempt
→ hiển thị đáp án tham khảo
→ learner bấm Nhận góp ý
→ AI Gateway
→ structured feedback
→ lưu feedback
```

Không tự gọi AI khi user chỉ mở/nhập câu.

## 5. AI feedback

Schema learner-facing:

- Ý nghĩa;
- Ngữ pháp;
- Dùng từ;
- Độ tự nhiên;
- Gợi ý sửa.

Prompt phải nhận HSK/context/rubric khi có để feedback phù hợp.

## 6. Invariants

- feedback chỉ cho own attempt;
- AI call có timeout;
- retry giới hạn;
- duplicate feedback request có idempotency/cache policy phù hợp;
- model failure không làm mất attempt;
- AI usage/cost được log.

## 7. Data model

```text
TranslationAttempt
TranslationFeedback
```

Không Event Source.

## 8. Commands / Queries

Commands:
- SubmitTranslationAttempt;
- RequestTranslationFeedback.

Queries:
- GetTranslationExercise;
- GetTranslationAttempt;
- GetTranslationHistory.

## 9. API gợi ý

```text
GET  /api/translation/exercises
POST /api/translation/exercises/{id}/attempts
POST /api/translation/attempts/{id}/feedback
GET  /api/translation/history
```

## 10. RabbitMQ

Không mặc định. Feedback learner-facing ưu tiên direct request nếu latency/provider cho phép.

Nếu sau này chuyển async phải giữ UX rõ trạng thái processing.

## 11. AI

Có — đúng use case đã duyệt.

## 12. Test cases

- [x] attempt saved without AI; `identity-smoke.mjs` và HTTP QA xác nhận attempt được lưu và phát `TranslationAttemptSignal` vào Progress;
- [x] AI only after explicit request; feedback chỉ gọi gateway ở endpoint `/feedback`, submit không gọi provider;
- [x] structured schema validation;
- [x] timeout/failure UI-safe;
- [x] permission own attempt;
- [x] Computer Use full flow.
