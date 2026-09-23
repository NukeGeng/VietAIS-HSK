# Exam Module

## 1. Mục đích

Quản lý thi thử HSK: attempt, navigation state, objective grading, subjective grading có chọn lọc và kết quả.

## 2. Phạm vi

- exam blueprint/reference;
- start/resume exam;
- answer submit;
- section progress;
- submit exam;
- objective grading;
- AI request cho subjective part phù hợp;
- final result;
- signal cho Review/Progress.

## 3. Actor & phân quyền

| Actor | Quyền |
|---|---|
| Guest | xem danh sách/sample nếu cho phép; không lưu full attempt |
| Learner | take/view own attempts |
| Admin | quản lý exam content qua Content; xem operational data theo permission riêng nếu cần |

## 4. Event Sourcing

Stream:

```text
ExamAttempt-{AttemptId}
```

Events baseline:

```text
ExamStarted
AnswerSubmitted
SectionCompleted
ExamSubmitted
ObjectiveScoreCalculated
SubjectiveGradingRequested
SubjectiveGradingCompleted
ExamScored
```

Không append event cho timer tick.

## 5. Flow

```text
StartExam
→ answer questions
→ optional save per answer
→ SubmitExam
→ objective score ngay
→ nếu subjective cần AI: enqueue
→ AI worker result
→ ExamScored
```

Nếu không có subjective part, final score không chờ AI.

## 6. Invariants

- learner chỉ access own attempt;
- submitted attempt không nhận answer mới;
- duplicate submit idempotent;
- AI result phải correlate đúng attempt/section;
- objective score không do AI chấm;
- exam content version/reference cố định tại lúc start để result reproducible.

## 7. Projections

### Inline

`ExamAttemptCurrent`:
- status;
- current answers;
- section state;
- submitted state;
- objective score khi có.

### Async

- ExamHistory;
- QuestionStatistics;
- skill/knowledge result signals cho Progress/Review.

## 8. Commands

| Command | Mục đích |
|---|---|
| StartExam | tạo stream |
| SubmitExamAnswer | append answer |
| CompleteExamSection | section state |
| SubmitExam | khóa bài + bắt đầu grading |
| ApplySubjectiveGradingResult | worker callback/message |

## 9. Queries

- GetExamCatalog;
- GetExamAttempt;
- GetExamResult;
- GetMyExamHistory.

## 10. API gợi ý

```text
GET  /api/exams
POST /api/exams/{examId}/attempts
GET  /api/exam-attempts/{id}
POST /api/exam-attempts/{id}/answers
POST /api/exam-attempts/{id}/submit
GET  /api/exam-attempts/{id}/result
POST /api/exam-attempts/{id}/subjective-grading
```

Đã triển khai thêm `GET /api/exams/{examId}` để màn hình chi tiết đề thi
không phải suy đoán dữ liệu từ catalog. Definition trả ra public question
view và `contentVersion`; không trả accepted answers hoặc event stream.

Exam question có thể mang `QuestionType` và reference `KnowledgeType/KnowledgeId`. Khi submit,
những reference này được phát trong `ExamResultSignal.KnowledgeResults` để Review/Progress dùng
đúng contract, không copy master data.

## 11. RabbitMQ

Có, chỉ khi cần subjective grading async:

```text
Wolverine Outbox
→ RabbitMQ ai.grading
→ AI Worker
→ SubjectiveGradingCompleted
```

Phải idempotent và correlation rõ.

## 12. AI

Chỉ subjective response cần hiểu ngôn ngữ tự nhiên.

Structured output cần schema version.

AI score phải được ghi rõ là practice evaluation, không giả là official CTI scoring nếu không có official scoring implementation.

## 13. Dependencies

- Content: exam/question definitions;
- Curriculum: knowledge/skill tags;
- Review/Progress: consume result signals;
- AI building block/worker.

## 14. Test cases

- [x] start/resume;
- [x] answer persistence;
- [x] refresh giữa bài;
- [x] double submit;
- [x] objective score deterministic;
- [x] local subjective job request có correlation/idempotency; [x] optional Wolverine/RabbitMQ
  adapter route message vào `vietais.exam.subjective-grading`; [x] HMAC-authenticated production
  worker callback boundary; [x] worker consumer process + fail-closed callback smoke; [ ] AI
  grading provider thật/deployment;
- [x] duplicate subjective result idempotent ở local callback seam;
- [x] permission own-attempt;
- [x] Computer Use full exam flow.

## 15. Acceptance Criteria

- [x] event replay reconstruct current state;
- [x] no objective AI grading;
- [x] refresh/resume ổn định;
- [x] result signal integrate Review/Progress; Progress tạo weak-point `ExamIncorrect`, Review
  nhận `ExamKnowledgeResult` cho câu sai và dedupe theo attempt/question.
