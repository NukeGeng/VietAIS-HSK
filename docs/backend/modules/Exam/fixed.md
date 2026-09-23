# Fixed

## EXAM-FIX-010 — Giữ feedback của subjective failure sau projection/rebuild

- Projection InMemory và Marten dùng kết quả cuối cùng của `SubjectiveGradingCompleted` hoặc
  `SubjectiveGradingFailed` để trả `SubjectiveScore`/`SubjectiveFeedback`.
- Thêm regression test `Subjective_failure_feedback_survives_attempt_rebuild`.
- Worker smoke đã bắt được lỗi này sau khi queue consumer/callback HMAC chạy thật; test phải kiểm
  tra cả trạng thái `Failed` và feedback, không chỉ status.

## EXAM-FIX-009 — HMAC-authenticated worker callback boundary

### Thay đổi

- Thêm `IWorkerCallbackAuthenticator`/HMAC-SHA256 boundary với headers
  `X-VietAIS-Worker-Timestamp` và `X-VietAIS-Worker-Signature: v1=<base64url>`.
- Chữ ký bao phủ payload nguyên bản theo format `<unixTimestamp>\n<body>`; timestamp mặc định
  chỉ hợp lệ trong 300 giây và so sánh chữ ký constant-time.
- Khi `Workers:Callback:SharedSecret` được cấu hình, callback không phụ thuộc learner/dev header
  và yêu cầu `UserId` trong payload đã ký. Khi secret trống, Development vẫn dùng adapter cũ cho
  local smoke; production không được coi dev header là callback auth.
- `JobId` correlation và duplicate result handling của Exam store vẫn giữ nguyên.

### Automated test và smoke

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass
  `103/103`, gồm valid/tampered/expired/extreme-timestamp/missing-secret signature tests và
  regression cho feedback của failure result.
- `identity-smoke.mjs` pass ở local dev adapter và pass với
  `WORKER_CALLBACK_SHARED_SECRET` trên API port `5057`.
- AI grading worker/provider thật chưa được giả lập là đã hoàn thành; chỉ callback transport
  boundary được triển khai.

## EXAM-FIX-001 — Bootstrap event-stream attempt and objective scoring

### Thay đổi

- Thêm exam catalog fixture với content version cố định tại lúc bắt đầu attempt.
- Lưu `ExamStarted`, `AnswerSubmitted`, `ExamSubmitted`, `ObjectiveScoreCalculated` trong stream `ExamAttempt-{AttemptId}` dạng bootstrap in-memory.
- Rebuild attempt state từ event stream để hỗ trợ resume và đọc result.
- Objective score deterministic; submitted attempt không nhận answer mới.
- Submit idempotent và attempt được cô lập theo UserId.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm start/resume, answer, cross-user isolation, submit twice, score và result.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Không dùng AI cho objective grading.
- Không dùng RabbitMQ khi exam không có subjective section.
- Subjective grading/outbox worker vẫn để bước sau khi có decision và content thật.

## EXAM-FIX-002 — Public exam views không lộ answer key/event stream

- Tách public definition/attempt view khỏi domain model.
- Catalog và attempt response chỉ trả prompt, answers learner đã nhập và score cần thiết.
- Smoke test assert không có `acceptedAnswers` và `events`.

## EXAM-FIX-003 — Marten event-stream persistence cho exam attempt

### Thay đổi

- Thêm `MartenExamStore` lưu `ExamAttemptMetadata` và stream
  `ExamAttempt-{AttemptId}` khi có Postgres/Marten.
- Rebuild `Active`, `Submitted` và `Scored` state từ `ExamStarted`,
  `AnswerSubmitted`, `ExamSubmitted` và `ObjectiveScoreCalculated`.
- Giữ content version và câu hỏi tại thời điểm start để result reproducible.
- Submit/answer vẫn kiểm tra ownership, submitted state và idempotency.
- Thêm `GET /api/exams/{examId}` cho màn hình detail.

### Verification

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` —
  pass `39/39`, gồm isolation, deterministic score và double-submit test.
- `node scripts/identity-smoke.mjs` — pass toàn bộ flow Identity → Curriculum →
  Learning → Practice → Review → Exam → Translation → Speaking.
- Local runtime không có Postgres connection string nên smoke test chạy trên
  `InMemoryExamStore`; Marten registration/build đã được compile và wiring kiểm tra.

## EXAM-FIX-004 — Exam result signal vào Progress

- Submit objective exam phát `ExamResultSignal` idempotent theo `exam:{attemptId}`.
- Progress nhận tổng câu đúng/sai, số attempt và danh sách câu sai; câu sai tạo weak-point `ExamIncorrect`.
- Submit lại attempt đã scored không cộng metrics/history lần hai.
- Automated: `ProgressStoreTests.Exam_result_signal_updates_metrics_and_exam_weak_point_once` — pass.

## EXAM-FIX-005 — Exam knowledge result vào Review

- Mở rộng `ExamQuestion` với `QuestionType`, `KnowledgeType`, `KnowledgeId` tùy chọn; public view
  chỉ trả `QuestionType`, không trả answer key.
- `ExamResultSignal` mang `KnowledgeResults`; khi submit, Exam gửi cùng signal tới Progress và
  Review. Review tạo `WrongAnswer` bằng reference đã được duyệt, không truy cập storage nội bộ
  của Curriculum.
- `ReviewItem.LastSourceEventId` giúp InMemory/Marten dedupe signal lặp theo attempt/question.
- Automated: `ReviewStoreTests.Exam_signal_maps_incorrect_knowledge_to_review_once` và
  `scripts/identity-smoke.mjs` — pass; test suite hiện 96/96.

## EXAM-FIX-006 — Subjective grading pending state và callback idempotency

- Thêm bootstrap subjective exam để kiểm tra tách objective scoring khỏi phần cần chấm
  ngôn ngữ tự nhiên.
- Khi submit, stream ghi `SubjectiveGradingRequested` với `JobId`, `CorrelationId` và
  `SchemaVersion`; local queue dedupe theo `JobId` và trả attempt ở trạng thái `Pending`.
- Thêm `POST /api/exam-attempts/{id}/subjective-grading` như local/test adapter seam.
  Result `Completed`/`Failed` được correlation với request; gửi lại cùng job chỉ rebuild
  state và không append event lần hai.
- Objective score và ExamResult chỉ tính câu objective; subjective score/feedback được
  trả riêng khi callback hoàn tất.

### Verification

- `ExamStoreTests.Subjective_exam_queues_once_and_applies_duplicate_result_idempotently` — pass.
- `scripts/identity-smoke.mjs` — pass sau khi rebuild API, gồm pending → completed và
  duplicate callback.
- Wolverine/RabbitMQ worker và authenticated production callback vẫn là adapter production
  cần cấu hình sau; local seam không được xem là AI provider thật.

## EXAM-FIX-007 — Optional Wolverine/RabbitMQ subjective grading adapter

- Khi bật production messaging config, `SubjectiveGradingRequestedMessage` được route vào queue
  `vietais.exam.subjective-grading`; Marten dùng durable inbox/outbox integration.
- Local mặc định vẫn giữ in-memory queue và callback seam để test state machine không phụ thuộc
  broker hay AI provider.
- Verification: `dotnet build VietAisHsk.slnx --no-restore`, `dotnet test ...` `96/96`, và
  `scripts/identity-smoke.mjs` sau khi restart API sạch — pass.

## EXAM-FIX-008 — Container messaging startup và scoped queue adapter

- Thêm `WolverineFx.RuntimeCompilation` cho API image để Wolverine khởi động được với dynamic
  handler generation.
- Subjective queue adapter resolve scoped `IMessageBus` qua `IServiceScopeFactory` ở boundary
  enqueue, giữ lifetime đúng khi `IExamStore` là singleton.
- Callback production boundary hiện đã dùng HMAC; worker consumer/provider và secret manager vẫn
  là phần triển khai vận hành tiếp theo.
- Compose QA 2026-09-23 đã provision queue `vietais.exam.subjective-grading`; `/health` trả 200
  và identity/learning smoke pass qua API container.
- AI grading worker thật vẫn là production follow-up, không ghi nhận local queue là provider AI.
