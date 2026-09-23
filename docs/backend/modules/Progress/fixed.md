# Fixed

## PROGRESS-FIX-008 — Tách kiểm tra persistence history theo learner

### Thay đổi

- Sửa `scripts/marten-persistence-smoke.mjs` để không gộp history của learner learning với learner practice/review.
- Pha read/verify kiểm tra `lesson-completed` bằng `learningHeaders`, còn `practice-completed` và `review-completed` bằng header learner persistence.
- Đồng thời xác nhận activity không bị lộ chéo giữa hai learner; đây là kiểm tra ownership của dữ liệu, không chỉ kiểm tra status `200`.

### Kiểm chứng

- PostgreSQL/Marten document inspection 2026-09-23 xác nhận hai learner có đúng activity riêng.
- Smoke `write → API restart/read → API restart/verify` chạy lại bằng learner identities tách biệt sau khi cập nhật harness.

## PROGRESS-FIX-004 — Review result signal và weak-point evidence

### Thay đổi

- Thêm `ReviewEvaluationSignal` làm contract giữa Review và Progress.
- `ReviewEndpoints` phát signal với event key ổn định theo `(reviewSessionId, reviewItemId)`; retry cùng kết quả không cộng lại projection.
- Projection lưu riêng Review signals, đếm `ReviewAnswered/Correct/Incorrect` và tính evidence điểm yếu theo rule: Review sai `+1`, Review đúng `-1`, clamp tối thiểu `0`.
- Marten và in-memory Progress đều preserve Review signals khi rebuild từ Practice/activity inputs.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 44/44.
- Test Review đúng loại bỏ weak point sau một lỗi trước đó; retry signal idempotent.
- Test Review sai tăng evidence và giữ reason `WritingWeak` cho Hanzi writing.

## PROGRESS-FIX-005 — Exam result signal

### Thay đổi

- Thêm `ExamResultSignal` lưu kết quả objective theo attempt, content exam, tổng câu và câu sai.
- `/api/exam-attempts/{id}/submit` phát signal idempotent theo attempt và ghi activity `exam-submitted`.
- Projection đếm `ExamAttempts/ExamQuestions/ExamCorrect/ExamIncorrect`; mỗi câu sai tạo weak-point `ExamIncorrect` để dẫn tới `Nội dung cần ôn`.
- Marten và in-memory Progress giữ Exam signals trong mọi lần rebuild/preserve document.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 49/49.
- Test duplicate Exam signal chỉ tạo một attempt metric và một weak-point.

### Giới hạn còn lại

- Exam signal chưa có mapping question → master vocabulary/grammar/Hanzi; weak-point dùng composite exam/question id và action tổng quát.

## PROGRESS-FIX-006 — Translation và Speaking activity signals

### Thay đổi

- Thêm `TranslationAttemptSignal` và `SpeakingSessionCompletedSignal` với event id ổn định.
- Translation submit cập nhật `TranslationAttempts` và history; Speaking end cập nhật `SpeakingSessions`, `SpeakingTurns` và history.
- In-memory/Marten projection đều dedupe signal/activity và preserve hai nhóm input khi rebuild.
- Không suy diễn correctness cho câu dịch và không tạo pronunciation score khi chưa có provider.

### Automated/HTTP kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 50/50.
- `scripts/identity-smoke.mjs` kiểm tra signal trong flow Translation/Speaking.
- HTTP QA 2026-09-22: một attempt + một speaking session (một turn), gọi end lần hai không duplicate; Progress trả `translationAttempts=1`, `speakingSessions=1`, `speakingTurns=1`.

## PROGRESS-FIX-007 — Hanzi writing weakness dẫn tới action luyện viết

### Kiểm chứng

- Hai lần hoàn tất Hanzi writing không đủ nét phát `hanzi-writing/Incorrect`, projection cộng evidence thành `WritingWeak`.
- Weak point giữ `knowledgeId=hanzi-yi` và action `Luyện viết`.
- Computer Use 2026-09-22: `/app/progress/weak-points` → `Luyện viết` mở đúng `/app/hanzi/hanzi-yi/write`; canvas và nút `Hoàn tất lượt viết` hiển thị.

## PROGRESS-FIX-003 — Learner timezone cho streak

- `/api/progress/streak` đọc timezone từ `LearnerProfile` của Identity thay vì hard-code `UTC`.
- Automated: `ProgressStoreTests.Streak_uses_learner_timezone_when_utc_dates_cross_midnight` — pass; `Asia/Ho_Chi_Minh` giữ đúng streak `2` khi hoạt động nằm sát nửa đêm UTC.

## PROGRESS-FIX-001 — Rule-based practice projection bootstrap

### Thay đổi

- Thêm progress snapshot, weak points, meaningful history và streak read endpoints.
- Practice answer phát signal vào Progress; câu sai tạo weakness có evidence/action.
- Practice completion ghi history theo activity key idempotent, không tính click/page view.
- Streak dùng qualifying activity và timezone fallback UTC ở bootstrap.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm practice projection, weak point và duplicate completion idempotency.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Progress không sở hữu attempt/source data.
- Không dùng AI.
- Projection hiện là in-memory bootstrap; replay/rebuild và Marten async projection vẫn là việc cần làm trước staging.

## PROGRESS-FIX-002 — Marten persistence, event idempotency và activity history

### Thay đổi

- Đăng ký `MartenProgressStore` khi có PostgreSQL; in-memory fallback vẫn dùng khi không có connection string.
- Lưu input practice signals, idempotent activity keys và read projection theo learner trong một `ProgressProjectionDocument`.
- Practice event identity ổn định theo session/question; khi answer của cùng câu trong session được thay thế, projection thay giá trị cũ thay vì cộng trùng.
- `ProgressProjectionBuilder` tái dựng snapshot, weak points và history từ inputs đã lưu.
- Nối lesson completion, practice completion và review-item result vào meaningful history; không ghi page view/click.

### Kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 24/24.
- `node --check scripts/marten-persistence-smoke.mjs` — pass.
- PostgreSQL smoke `write → API restart/read → API restart/verify` — pass: practice counters/weak point, lesson/practice/review history, user isolation và replay sau hai lần restart.
- Smoke gửi lại cùng answer trong cùng session và xác nhận `PracticeAnswered` không tăng trùng.
- PostgreSQL local giữ fixture/learner `marten-progress-smoke-20260922`; không xóa dữ liệu smoke.

### Giới hạn còn lại

- Projection hiện được ghi inline đồng bộ; Wolverine local messaging/async projection và operational rebuild tooling chưa được triển khai.
- Ở thời điểm fix này async projection/rebuild tooling vẫn pending; Exam/Translation/Speaking signal và mastery model deterministic đã được nối vào projection.
- Streak endpoint lấy timezone từ Identity; `UserKnowledgeMastery` dùng rule version hiện tại: score từ kết quả Practice + Review, `Strong >= 80`, `Learning >= 50`, còn lại `NeedsReview`.
- `PROGRESS-BE-FIX-004` — bổ sung `UserKnowledgeMastery` vào projection từ kết quả Practice + Review, có attempt/correct/incorrect/score/state deterministic; thêm `GET /api/progress/mastery` và giữ query hoạt động cho cả InMemory/Marten store.
- `PROGRESS-BE-FIX-005` — Marten Progress store giữ nguyên Translation/Speaking signals khi projection được rebuild bởi Practice, Review hoặc Exam signal; tránh làm mất metrics sau lần cập nhật tiếp theo.
