# Fixed

## PRACTICE-FIX-001 — Deterministic session and grading bootstrap

### Thay đổi

- Thêm practice session, question attempt, deterministic accepted-answer grading và session result.
- Session/query/answer/complete đều lấy `UserId` từ trusted context và không nhận user ID từ client.
- Complete session idempotent; truy cập session của user khác trả 404.
- Chuẩn hóa enum response thành chuỗi JSON (`Correct`, `Incorrect`, `NeedsRetry`).
- Giữ question catalog fixture tách qua reader contract; dữ liệu HSK chính thức vẫn thuộc Content.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm auth, grading đúng/sai, isolation và complete idempotency.
- `dotnet test VietAisHsk.slnx --no-restore -m:1` — pass, gồm normalization và accepted-answer grading.

### Computer Use

- Production frontend đã có Hanzi writing canvas ở `HanziView`; phần learner attempt/geometry/result được nối tiếp và hoàn tất ở `PRACTICE-FIX-005`.

### Kết quả

- Practice grading không gọi AI và không dùng RabbitMQ.
- API trả lỗi rõ ràng cho session không tồn tại, answer thiếu và session đã hoàn thành.

## PRACTICE-FIX-002 — Public practice views không lộ answer key

- Tách `PracticeQuestionView` và `PracticeSessionView` khỏi domain model.
- Thêm endpoint question catalog chỉ trả prompt/type/status.
- Smoke test assert response không có `acceptedAnswers`.

## PRACTICE-FIX-003 — Marten persistence cho practice session

### Thay đổi

- Đăng ký `MartenPracticeStore` khi có `ConnectionStrings:Postgres`; local không có DB tiếp tục dùng in-memory fallback.
- Lưu `PracticeSession` như Marten document, bao gồm private answer key, attempts và trạng thái; public endpoints vẫn map qua view không có `acceptedAnswers`.
- Giữ owner isolation, deterministic grading, answer replacement và completion idempotency của contract hiện có.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 24/24.
- `scripts/marten-persistence-smoke.mjs`: write active session + answer → API restart/read → complete → API restart/verify — pass.
- Restart smoke xác nhận status/answer được replay, public view không lộ answer key và user khác nhận 404.

### Giới hạn còn lại

- Question bank thật vẫn thuộc Content và chưa được import; smoke dùng `bootstrap-vocab-hello`.
- Review is durable through `REVIEW-FIX-002`; Progress projection is durable through `PROGRESS-FIX-002`.
- Hanzi canvas ở thời điểm fix này là UI reference; attempt/validator được triển khai tiếp ở `PRACTICE-FIX-005`.
- PostgreSQL local giữ session smoke `a64df574ed1c4f708769c0afbb2f52e9` và learner `marten-persistence-user`; không xóa dữ liệu database.

## PRACTICE-FIX-004 — Stable identity for progress result signals

- Mỗi signal dùng key `practice:{sessionId}:{questionId}` để Progress thay attempt hiện tại khi answer được gửi lại trong cùng session.
- Practice vẫn chấm deterministic; public response/session contract không đổi.
- PostgreSQL smoke xác nhận gửi cùng answer hai lần không làm tăng `PracticeAnswered`.

## PRACTICE-FIX-005 — Deterministic Hanzi writing attempt and feedback

### Thay đổi

- Thêm `HanziWritingAttempt`, mode `Guided`/`Trace`/`Recall`, per-stroke result và overall result.
- Thêm API start/get/submit-stroke/complete dưới `/api/practice/hanzi/...`.
- Validator kiểm tra số điểm tối thiểu, vùng vẽ, điểm đầu/cuối và hướng nét theo reference path; không dùng AI và không sinh score 0–100.
- Attempt ghi `StrokeSourceVersion`; khi PostgreSQL có cấu hình, Marten lưu attempt qua process restart.
- Kết quả không đạt gửi `WritingWeak` sang Review và Progress; kết quả đạt vẫn ghi activity hoàn tất.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 29/29.
- Smoke đúng nét: `Correct`, đủ 3/3 nét, complete idempotent.
- Smoke sai nét: `NeedsRetry`, feedback điểm bắt đầu, Review `WritingWeak`, Progress action `Luyện viết`.

### Computer Use

- Desktop: vẽ sai → nhận feedback → retry đúng → đủ nét → complete — pass.
- Frontend production build/type-check — pass.

### Giới hạn còn lại

- Reference hiện vẫn là `PlatformAuthoredReferenceFixture` tối thiểu; cần import dataset HSK/stroke chính thức đã duyệt.
- Mobile viewport và persistence smoke qua restart cho chính Hanzi writing cần bổ sung vào regression run kế tiếp.

## PRACTICE-FIX-006 — Skill-filtered deterministic question flow

### Thay đổi

- Mở rộng bootstrap question reader cho vocabulary, tone, Hanzi, grammar, listening, reading và
  writing; dữ liệu vẫn được ghi rõ là fixture platform-authored, không phải official HSK dataset.
- `GET /api/practice/questions?type=...` lọc theo skill và trả public view không có answer key.
- Nối `/app/skills/listening`, `/app/skills/reading` và `/app/skills/writing` vào `PracticeView`
  thay vì PageView mô tả; giữ Nói/Dịch ở module riêng.
- Nhãn UI map mã nội bộ sang tiếng Việt (`Nghe`, `Đọc`, `Viết`, …).

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 36/36.
- `npm run build` — pass.
- Runtime API với `X-Dev-User-Id`: listening/reading/writing/hanzi mỗi loại trả đúng 1 câu;
  response public không có `acceptedAnswers`.

### Computer Use

- `/app/skills/listening`: bắt đầu → nhập `ở lớp học` → `Correct` → hoàn tất `1/1` — pass.

### Giới hạn còn lại

- Bộ câu hỏi HSK 3.0 chính thức, audio/transcript thật và mobile 390px visual regression vẫn thuộc
  các hạng mục Content/data và QA tiếp theo; question bank hiện đã có public/admin Content contract
  và giữ `ContentVersion` khi tạo Practice session.

## PRACTICE-FIX-007 — Practice đọc question bank qua Content boundary

- Tách bootstrap catalog khỏi `PracticeStore.cs` sang `Modules/Content/BootstrapQuestionBank`.
- Practice chỉ giữ session, attempt và deterministic grading; `ContentPracticeQuestionReader` map
  published question sang model private có answer key.
- `GET /api/practice/questions` vẫn trả public view không có `acceptedAnswers`.
- Practice question view/session giữ `contentVersion` để reference không đổi trong phiên đang học.
- `dotnet test ... --no-restore` — pass sau khi tách boundary.

## PRACTICE-FIX-008 — Multiple-choice options cho Practice

### Thay đổi

- Content question có `Options` public-safe; accepted answer vẫn chỉ nằm trong private Practice question.
- Bootstrap tone/Hanzi/ngữ pháp questions có lựa chọn; Practice API map lựa chọn sang public session view.
- `PracticeView` render option buttons với trạng thái selected, còn câu tự do tiếp tục dùng text input.
- Grading vẫn so khớp deterministic với accepted-answer set; không gọi AI.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 48/48.
- Test kiểm tra options đi qua Content → Practice nhưng không thay đổi provenance/answer-key boundary.

### Giới hạn còn lại

- Options hiện là platform-authored fixture; question bank HSK chính thức vẫn pending.

## PRACTICE-FIX-009 — Content publish boundary và version reference

### Thay đổi

- Practice chỉ chọn câu hỏi có `Status = Published` từ Content; draft không thể lọt vào learner session.
- `ContentVersion` được copy vào `PracticeQuestion` và `PracticeQuestionView`, giữ snapshot version khi session bắt đầu.
- Public question/session response không trả `AcceptedAnswers`; admin Content mới được xem và publish question.
- Hanzi attempt lưu `StrokeSourceVersion` khi bắt đầu và từ chối submit/complete nếu reference đã đổi.

### Kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 60/60.
- `node scripts/identity-smoke.mjs` — pass, gồm public question visibility, draft publish, immutable published question và Practice flow.

## PRACTICE-FIX-010 — Computer Use regression cho skill practice

### Computer Use production QA — 2026-09-22

- `/app/practice/pinyin`: bắt đầu → nhập `nihao` cho `你好` → `Correct` → hoàn tất `1/1`.
- `/app/practice/tone`: bắt đầu → chọn `A 1` cho `mā` → `Correct` → hoàn tất `1/1`.
- `/app/practice/vocabulary`: bắt đầu → nhập `xin chào` cho `你好` → `Correct` → hoàn tất `1/1`.
- `/app/hanzi/hanzi-da/write` ở mode `Guided`: vẽ sai → nhận `Điểm bắt đầu chưa đúng vị trí.` → `Làm lại` → vẽ nét đúng → nhận `1 / 3 nét` và `Nét đúng. Tiếp tục nét tiếp theo.`.

Các flow đều chạy qua API production dev đang dùng `X-Dev-User-Id`, chỉ lấy câu hỏi `Published`, không lộ answer key và không gọi AI cho grading.
