# Fixed

## LEARNING-FIX-009 — Completion unit/level trong HSK read model

- GET learning/hsk/{level} bổ sung completedUnitIds và isLevelCompleted, derive từ lesson completion và curriculum Published hiện tại. Không thay đổi event history.
- Unit/level rỗng không hoàn tất; bài khác unit và bài Draft không tạo completion giả.
- 3 unit tests mới; toàn suite 74/74 pass. HTTP smoke kiểm tra level false trước complete, đúng unit ID và level true sau complete — pass trên API riêng 5057.
- Chưa nối trạng thái mới vào Vue; chưa phải milestone lịch sử bất biến/chứng nhận cấp độ.

## LEARNING-FIX-008 — Resume theo track đang hoạt động

- Sửa LEARNING-BUG-004: Beginner không còn resume nhầm lesson HSK được giữ trong state.
- HTTP regression trên API riêng 5057: HSK lesson → Beginner trả `beginner/tones`, current lesson vẫn được giữ → chọn lại HSK trả đúng lesson cũ. Pass.
- `dotnet test tests/VietAisHsk.Api.Tests --no-restore -m:1`: 60/60 pass; `git diff --check`: pass.
- Test nằm trong `scripts/learning-completion-smoke.mjs`; đã xác nhận fail trước fix, pass sau fix.
- Chưa retest thao tác đổi track qua UI trong lần này. API preview 5055 đang chạy bản cũ; bản sửa được build và kiểm thử riêng, chưa restart preview để tránh xóa state in-memory đang dùng.

## LEARNING-FIX-007 — Chặn completion không hợp lệ trước khi ghi Progress

- Endpoint trả 409 nếu lesson chưa bắt đầu hoặc Beginner stage chưa phải bước hiện tại đã bắt đầu.
- Complete lặp lại trả state đã hoàn thành, không ghi lại timestamp hoạt động.
- Tái hiện trước fix: hoàn tất Thanh điệu khi chưa học trả 200 và tạo activity sai.
- Retest HTTP trên API riêng cổng 5057 bằng `scripts/learning-completion-smoke.mjs`: chưa bắt đầu → 409, skip stage → 409, hợp lệ → 200, retry → state không đổi; history chỉ có hai completion hợp lệ.
- Script tạo ID riêng mỗi lần chạy; dùng development API test vì có import/publish fixture curriculum.

## LEARNING-FIX-001 — Bootstrap learner path state

### Thay đổi

- Thêm learner state cho beginner/HSK context, current stage/lesson và started/completed lesson IDs.
- Thêm `GET /api/learning/home`, beginner start/view, HSK select/view và lesson start/complete routes.
- Beginner start idempotent và bắt đầu ở stage `pinyin`.
- Lesson chỉ được mutate khi Curriculum xác nhận lesson đã Published.
- Complete lesson idempotent; chưa start thì trả business conflict.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm beginner start, continue target, HSK selection và unpublished lesson guard.

### Computer Use

- Chưa áp dụng; production frontend chưa được triển khai.

### Kết quả

- Learning state không nhận `UserId` từ request body; lấy từ trusted user context.
- Beginner và HSK context không dùng AI/RabbitMQ.
- Chưa đánh dấu module hoàn tất vì storage hiện là bootstrap in-memory và chưa có event replay/Marten persistence.

## LEARNING-FIX-002 — Marten event stream cho learner state

### Thay đổi

- Thêm `LearningTrackStarted`, `HskLevelSelected`, `LessonStarted`, `LessonCompleted` và reducer thuần để replay state theo từng learner.
- `MartenLearningStore` lưu một stream `LearningPath-{UserId}`; transition idempotent không append milestone trùng.
- Khi có `ConnectionStrings:Postgres`, DI dùng Marten store; không có connection string thì tiếp tục dùng in-memory local fallback.
- Append dùng expected next event version để Marten phát hiện concurrent write, và event read nạp toàn bộ stream.

### Automated test

- `dotnet build tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 0 warning.
- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-build --no-restore -m:1` — pass, 15/15.
- `scripts/marten-persistence-smoke.mjs`: write Beginner + HSK selection → dừng API → khởi động process mới → đọc lại learner state — pass.

### Giới hạn còn lại

- Computer Use flow và UI continue-learning chưa được kiểm thử trong lần fix này.
- Module Learning vẫn chưa hoàn tất toàn bộ acceptance criteria.

## LEARNING-FIX-003 — Published curriculum lesson start/complete qua Marten

### Thay đổi

- Curriculum import/publish hiện lưu Topic/Unit/Lesson bền vững và `IsPublishedLesson` chặn lesson draft.
- Learning chỉ start/complete lesson Published; lesson start và complete được replay sau khi API khởi động lại.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 18/18.
- `scripts/marten-persistence-smoke.mjs`: write → API restart/read + complete → API restart/verify; published tree, draft guard, current lesson resume và completed lesson replay — pass.

### Giới hạn còn lại

- Chưa có HSK syllabus chính thức được import, nên test dùng fixture `smoke-test`.
- Computer Use flow và UI continue-learning vẫn chưa được kiểm thử; Learning chưa hoàn tất acceptance criteria.
- Smoke learner id `marten-learning-tree-20260922` được giữ trong PostgreSQL local; truyền cùng `MARTEN_LEARNING_SMOKE_USER_ID` qua cả ba pha.

## LEARNING-FIX-004 — Computer Use QA cho HSK lesson flow

### Kiểm tra

- Import/publish fixture curriculum có topic, unit và hai lesson đã publish.
- Mở `/app/hsk` → HSK 3 → lesson `Chào hỏi cơ bản`.
- Bấm `Bắt đầu bài học` → `Hoàn thành bài học`.
- Tải lại trang detail; trạng thái vẫn là `Đã hoàn thành`.
- Gọi lại complete sau khi đã hoàn thành không tạo activity progress trùng nhờ activity event key ổn định.

### Kết quả

- Computer Use trên Vite/API local — pass ngày 2026-09-22.
- `GET /api/learning/home` giữ `startedLessonIds` và `completedLessonIds` sau reload.
- `GET /api/progress` giữ một `lesson-completed` activity cho lesson dù complete lặp lại.

### Giới hạn còn lại

- Beginner → bài đầu → complete → next và continue-learning card vẫn chưa đạt acceptance criteria.
- Fixture QA không thay thế HSK syllabus chính thức.

## LEARNING-FIX-005 — Beginner stage progression có thứ tự

### Thay đổi

- Thêm event `BeginnerStageStarted` và `BeginnerStageCompleted` cùng các tập stage đã bắt đầu/đã hoàn tất trong learner state.
- Chỉ cho bắt đầu stage hiện tại; không cho skip stage hoặc mutate state trước khi trả conflict.
- Hoàn tất Pinyin tự chuyển sang Thanh điệu đang Published; activity `beginner-stage-completed` dùng event key ổn định.
- Thêm API `POST /api/learning/beginner/stages/{id}/start` và `/complete`.

### Verification

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 54/54.
- `npm run build` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm first-stage start, skip-stage guard, complete và continue target.
- Computer Use: `/app/pinyin` bắt đầu → hoàn tất → `/app/tones` hiển thị bước kế tiếp; cả hai activity xuất hiện đúng một lần trong Progress — pass ngày 2026-09-22.

### Giới hạn còn lại

- Các stage Beginner từ Âm đầu/Vần trở đi vẫn Draft vì chưa có nội dung đã duyệt.
- Mobile QA riêng của HomeView vẫn còn pending.

## LEARNING-FIX-006 — Continue-learning target được nối vào HomeView

### Verification

- `GET /api/learning/home` trả `continueTarget` theo lesson đang học; fixture `qa-home-lesson` được publish trước khi start.
- HomeView hiển thị action `Tiếp tục học →` và ưu tiên lesson đang dở trong danh sách bài học tiếp theo.
- Computer Use QA 2026-09-22: bấm action từ `/app` chuyển đúng `/app/lessons/qa-home-lesson`; không đổi business flow hay sidebar context.

### Giới hạn còn lại

- Mobile QA riêng của HomeView vẫn còn pending.
- Fixture QA không thay thế HSK syllabus chính thức.
