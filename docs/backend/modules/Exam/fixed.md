# Fixed

## EXAM-FIX-001 — Bootstrap event-stream attempt and objective scoring

### Thay đổi

- Thêm exam catalog fixture với content version cố định tại lúc bắt đầu attempt.
- Lưu `ExamStarted`, `AnswerSubmitted`, `ExamSubmitted`, `ObjectiveScoreCalculated` trong stream `ExamAttempt-{AttemptId}` dạng bootstrap in-memory.
- Rebuild attempt state từ event stream để hỗ trợ resume và đọc result.
- Objective score deterministic; submitted attempt không nhận answer mới.
- Submit idempotent và attempt được cô lập theo UserId.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm start/resume, answer, cross-user isolation, submit twice, score và result.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Không dùng AI cho objective grading.
- Không dùng RabbitMQ khi exam không có subjective section.
- Subjective grading/outbox worker vẫn để bước sau khi có decision và content thật.
