# Fixed

- `CONTENT-BE-FIX-001` — thêm `IExtendedContentStore` và các catalog Story/Video/LearningResource/ToolDefinition có `Status`, provenance, HSK/topic tags; learner queries chỉ trả `Published`, admin endpoint xem draft/publish yêu cầu `content.manage`.
- API: `GET /api/content/stories`, `/videos`, `/resources`, `/tools` và detail routes; `GET /api/admin/content/{kind}`; `POST /api/admin/content/{kind}/{id}/publish`.
- Automated: `ExtendedContentStoreTests`, backend build/test 58/58 và `scripts/identity-smoke.mjs` kiểm tra filter, draft isolation, permission và publish visibility — pass.

- `CONTENT-BE-FIX-002` — question bank có public read view và admin draft/publish flow:
  `GET /api/content/questions`, `GET /api/admin/content/questions`,
  `POST /api/admin/content/questions/{id}/draft` và
  `POST /api/admin/content/questions/{id}/publish`. Public response không chứa `AcceptedAnswers`,
  draft không lộ tới learner, và câu đã Published không bị sửa tại chỗ.

- Computer Use QA 2026-09-22: route `/app/skills/listening` tải đúng câu hỏi Published từ
  Content → bắt đầu phiên → nhập `ở lớp học` → `Correct` → hoàn tất `1/1` — pass.
- Computer Use QA 2026-09-22: learner mở Story `story-classroom`, Video `video-classroom`,
  Resource `resource-hsk3-grammar` và Tools `tool-quick-lookup`; các route chỉ hiển thị record
  Published, draft không xuất hiện — pass.

## CONTENT-FIX-001 — Question bank contract cho Practice

### Thay đổi

- Thêm `ContentQuestion` và `IQuestionBank` trong `Modules/Content`.
- Đưa bootstrap question catalog vào `BootstrapQuestionBank`; record có type, status và provenance
  (`PlatformAuthoredReferenceFixture`, `question-reference-v1`, `platform-authored`).
- Practice chỉ dùng `ContentPracticeQuestionReader` adapter để map content question sang private
  grading model; public API vẫn không lộ `AcceptedAnswers`.
- Giữ filter skill và published-only query ở Content contract.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass.
- Test xác nhận filter type, dedupe id query và provenance.

### Giới hạn còn lại

- Catalog hiện là fixture platform-authored; import bộ câu hỏi HSK 3.0 chính thức vẫn chờ source
  được duyệt. Workflow draft/publish và content version bootstrap đã có để nhận dataset sau này.

## CONTENT-BE-FIX-003 — Audio generation contract và retry idempotent

### Thay đổi

- Thêm `AudioAssetStatus` (`Pending`, `Processing`, `Ready`, `Failed`) và `AudioAsset` có
  `AttemptCount`, `IdempotencyKey`, `AudioUrl`, `LastError`.
- Thêm `InMemoryAudioGenerationQueue` và `InMemoryAudioAssetStore` làm local seam: request trùng
  idempotency key không enqueue thêm message; key dùng cho payload khác trả conflict; retry chỉ
  chuyển `Failed` sang `Pending` với attempt/key mới.
- Thêm public ready-audio query và admin list/request/retry routes với `content.manage`.
- Không gọi provider khi learner đọc audio; adapter Wolverine/RabbitMQ, CosyVoice và storage thật
  vẫn được giữ là production follow-up.

### Automated và Computer Use QA

- `AudioAssetTests`: request duplicate, conflict, retry attempt và state transition — pass.
- `scripts/identity-smoke.mjs`: permission, ready-only learner read, create/duplicate/conflict và
  retry flow — pass.
- Backend test suite: 94/94 pass.
- Computer Use 2026-09-22: Admin → Audio hiển thị asset `Failed`; bấm `Thử lại` chuyển asset sang
  `Pending`, tăng lần thử lên 2 và hiện notice enqueue — pass.

## CONTENT-BE-FIX-004 — Optional Wolverine/RabbitMQ audio adapter

- Khi `Messaging:EnableWolverine=true` và có đủ Postgres/RabbitMQ connection string, API tích hợp
  Marten với Wolverine và route `AudioGenerationRequested` vào queue
  `vietais.audio.generate`.
- Local mặc định vẫn dùng in-memory queue; không làm smoke test hoặc Computer Use phụ thuộc broker.
- Worker CosyVoice, storage thật và operational dead-letter vẫn được ghi rõ là production follow-up.
- Verification: `dotnet build VietAisHsk.slnx --no-restore`, `dotnet test ...` `96/96`, và
  `scripts/identity-smoke.mjs` sau khi restart API sạch — pass.

## CONTENT-BE-FIX-005 — Container messaging startup và scoped queue adapter

- Thêm `WolverineFx.RuntimeCompilation` để dynamic handler generation hoạt động trong image API.
- Queue adapter singleton resolve `IMessageBus` qua scope mới cho mỗi lần enqueue, tránh inject
  trực tiếp scoped bus vào singleton store.
- Compose QA 2026-09-23: PostgreSQL/RabbitMQ healthy, API `/health` 200, audio queue được
  provision, identity và learning completion smoke pass.
- CosyVoice worker và object storage thật vẫn chưa được giả lập là đã hoàn thành; đây là phần
  production follow-up được ghi ở `docs/deploy/03-rabbitmq.md` và `docs/deploy/11-storage-cdn.md`.

## CONTENT-BE-FIX-006 — Persist Content state qua API restart

- Khi PostgreSQL/Marten được cấu hình, Content không còn reset sau API
  restart: question bank, Story/Video/Resource/Tool và AudioAsset được seed một lần rồi đọc/ghi
  qua document stores (`MartenQuestionBank`, `MartenExtendedContentStore`,
  `MartenAudioAssetStore`). Không có Postgres vẫn giữ fallback in-memory cho local development.
  Dataset HSK chính thức, CMS authoring đầy đủ, CosyVoice worker và object storage thật vẫn chưa
  được tuyên bố hoàn thành.

### Verification

- Compose persistence smoke 2026-09-23 trên stack dữ liệu sạch: write → restart/read →
  restart/verify đều pass; Story draft publish, question draft publish và AudioAsset Pending
  idempotency state được giữ qua hai lần API restart.
