# Fixed

## CURRICULUM-FIX-010 — Bảo vệ level Id giữa các syllabus version

### Thay đổi

- In-memory và Marten curriculum store kiểm tra xung đột trước khi ghi bất kỳ thay đổi nào.
- Nếu một HSK level Id đã thuộc version khác, import trả `409 Conflict` với lỗi có version cũ/mới;
  không silently đổi `SyllabusVersionId` của level đã tồn tại.
- Giữ retry cùng version và cùng payload là idempotent; version mới phải dùng level Id riêng theo
  version để tránh route/publish bị mơ hồ.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1`
  pass `98/98`, bao gồm regression `New_version_cannot_reuse_a_level_id_from_another_version`.
- API smoke trên port `5056`: import/publish `guard-v1`, import `guard-v2` dùng lại level Id trả
  `409`, và admin read xác nhận level vẫn trỏ về `guard-v1`.
- `git diff --check` pass.

## CURRICULUM-FIX-008 — Beginner master-data references and UI regression

- `BeginnerStage` now exposes `masterDataRefs`; published Pinyin/Tone stages reference
  `foundation:pinyin` and `foundation:tones`, while later stages declare their Hanzi/Pinyin
  dependencies without being falsely published.
- Hanzi catalog tests require non-empty source, version, license, stroke path and description.
- `identity-smoke.mjs` asserts the two published Beginner stages keep their master-data references
  and permission boundary.
- Computer Use QA 2026-09-22: Beginner rendered fully on desktop and 390×844; opened Pinyin and
  Thanh điệu from the stage list without clipping. Vocabulary HSK 3 filter reduced the catalog to
  2 records; Grammar HSK 3 filter rendered all 4 HSK 3 structures on mobile.
- Automated: .NET tests `90/90`, Vue build pass, smoke pass.

## CURRICULUM-FIX-009 — Admin level read contract

- Thêm `GET /api/admin/curriculum/hsk-levels`, yêu cầu `curriculum.manage`, trả cả Draft và
  Published để admin không phải suy luận từ public-only endpoint.
- In-memory và Marten Curriculum store dùng cùng contract, sắp xếp theo level/version update.
- Smoke xác nhận learner nhận `403`, admin thấy draft; Computer Use xác nhận admin publish level
  thành công từ `AdminView`.

## CURRICULUM-FIX-001 — Bootstrap versioned curriculum read/import flow

### Thay đổi

- Thêm model `SyllabusVersion`, `HskLevel`, beginner stages và foundation catalogs.
- Thêm import validation cho version, provenance, duplicate key/level và level range 1–9.
- Import ở trạng thái Draft; public query chỉ trả nội dung Published.
- Thêm publish flow yêu cầu capability `curriculum.manage`.
- Giữ HSK data thật ngoài code cho tới khi có dataset/provenance đã được duyệt.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm draft visibility/import permission/publish flow.

### Computer Use

- Chưa áp dụng; production frontend chưa được triển khai.

### Kết quả

- Beginner path trả đúng các stage structural theo product flow.
- Draft curriculum không lộ qua public endpoint.
- Published HSK level hiển thị qua `/api/curriculum/hsk-levels`.

## CURRICULUM-FIX-002 — Marten-backed version/level persistence

### Thay đổi

- Thêm `CurriculumDocument` lưu `SyllabusVersion` và `HskLevel` trong Marten.
- Khi có `ConnectionStrings:Postgres`, Curriculum store chuyển sang Marten; local không có DB vẫn dùng bootstrap store rõ ràng.
- Import/publish giữ trạng thái version và level, public read chỉ trả version Published.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- Marten smoke: import/publish trước restart, `hsk-levels` sau restart — pass.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Curriculum version/level đã được kiểm tra persistence qua process boundary.
- Ở thời điểm fix này, Topic/Unit/Lesson và master data chi tiết chưa có; cấu trúc Topic/Unit/Lesson đã được triển khai ở `CURRICULUM-FIX-004`, còn HSK dataset/provenance chính thức vẫn chờ nguồn được duyệt.

## CURRICULUM-FIX-003 — Versioned Pinyin and tone foundation catalogs

### Thay đổi

- Thay catalog rỗng bằng dữ liệu nền tảng dùng chung cho in-memory và Marten stores.
- Pinyin API có 21 âm đầu và 35 vần; mỗi mục có nhóm, giải thích, ví dụ và reading metadata.
- Tone API có bốn thanh, thanh nhẹ và ghi chú biến điệu thanh 3 trong lời nói liền.
- Catalog ghi `PlatformAuthored`, version và nguồn đối chiếu; không gắn nhãn dữ liệu HSK/CTI chính thức.
- Beginner track chỉ publish Pinyin và Thanh điệu đang có nội dung; các stage còn lại giữ Draft thay vì báo sẵn sàng giả.

### Automated test

- `dotnet test VietAisHsk.slnx --no-restore -m:1` — pass, 11/11.
- `node scripts/identity-smoke.mjs` — pass, gồm Beginner status và hai foundation endpoints.

### Kết quả

- Pinyin và thanh điệu có thể được đọc qua API public cho flow Beginner.
- Bộ âm tiết hợp lệ đầy đủ, audio và question bank vẫn nằm ngoài phạm vi của catalog nền tảng này.

## CURRICULUM-FIX-004 — Persisted HSK tree and lesson publication

### Thay đổi

- Import contract nhận cấu trúc lồng `Topic → Unit → Lesson`; mọi lesson mới được tạo Draft.
- In-memory và Marten stores cùng trả cây Published; Topic/Unit không có lesson Published bị ẩn khỏi learner.
- Thêm admin endpoint publish lesson, yêu cầu `curriculum.manage` và HSK level/version cha đã Published.
- `IsPublishedLesson` kiểm tra đúng version, level và lesson status để Learning chỉ nhận lesson Published.
- Import giống hệt trên version Published là idempotent; thay đổi nội dung trả `409` và yêu cầu version mới.
- Không sinh hoặc gắn nhãn dữ liệu HSK chính thức giả; smoke fixture được đánh dấu `smoke-test`.

### Automated test

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 18/18, không warning.
- `node --check scripts/marten-persistence-smoke.mjs` — pass.
- `scripts/marten-persistence-smoke.mjs` write → restart/read + complete → restart/verify — pass; gồm draft visibility, publish lesson, permission `403`, published mutation `409`, lesson start resume và completion replay qua process mới.

### Kết quả

- GET HSK tree chỉ lộ lesson Published; Learning bắt đầu/hoàn thành lesson đó và replay lại trạng thái từ Marten sau restart.
- HSK dataset, vocabulary/hanzi/grammar references và provenance chính thức vẫn chưa được nạp; smoke fixture không thay thế dữ liệu sản phẩm.
- PostgreSQL local giữ lại fixture `marten-hsk-3.0-tree-smoke` và learner `marten-learning-tree-20260922` để smoke có thể chạy lại; không xóa dữ liệu database. Dùng cùng `MARTEN_LEARNING_SMOKE_USER_ID` trong ba pha write/read/verify.

## CURRICULUM-FIX-005 — Hanzi reference read contract

### Thay đổi

- Thêm `HanziCharacter`, `HanziStroke`, `HanziStrokeSet` và `IHanziCatalog` trong Curriculum.
- Thêm `GET /api/hanzi`, `GET /api/hanzi/{id}` và `GET /api/hanzi/{id}/strokes`.
- Cung cấp fixture có provenance rõ ràng cho `一`, `人`, `大` để kiểm tra contract và renderer; không gắn nhãn HSK chính thức.
- Giữ learner writing attempt ngoài Curriculum; Practice sẽ sở hữu persistence/validator khi contract được duyệt.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 26/26.

### Kết quả

- Frontend có thể đọc catalog và stroke reference ổn định qua public contract.
- Dữ liệu HSK 3.0 chính thức, import/publish thật và geometry validator vẫn là công việc tiếp theo.

## CURRICULUM-FIX-006 — Vocabulary/Grammar reference read contracts

### Thay đổi

- Thêm `VocabularyEntry`, `VocabularyExample`, `GrammarPoint` và `GrammarExample` với status,
  HSK/topic mapping, examples và provenance.
- Thêm `GET /api/vocabulary`, `GET /api/vocabulary/{id}`, `GET /api/grammar` và
  `GET /api/grammar/{id}`.
- List hỗ trợ lọc `search`, `hsk`, `topic`; detail tra theo id hoặc khóa nội dung.
- Giữ dữ liệu trong `BootstrapKnowledgeCatalog` là platform-authored reference fixture, không
  gắn nhãn bộ dữ liệu HSK 3.0 chính thức.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass, 0 warning.
- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 31/31.
- API smoke sau restart binary mới: vocabulary list/detail/filter và grammar list/detail — pass.

### Kết quả

- Frontend có read contract ổn định cho hai nền tảng Vocabulary và Grammar.
- Official HSK dataset/import/publish và audio asset vẫn chưa được triển khai; không coi fixture là
  dữ liệu sản phẩm cuối.

## CURRICULUM-FIX-007 — Published lesson read contract

### Thay đổi

- Thêm `ICurriculumStore.GetPublishedLesson` cho cả in-memory và Marten store.
- Thêm `GET /api/curriculum/lessons/{id}`; endpoint chỉ trả lesson thuộc HSK level/version đã publish.
- Draft lesson, lesson không tồn tại hoặc lesson không thuộc cây Published trả `404`.
- Giữ dữ liệu import dùng cho UI flow là `PlatformAuthoredReferenceFixture`, không gắn nhãn HSK chính thức.

### Automated test và QA

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 38/38.
- API local đọc `lesson-ui-welcome` sau publish — pass.
- Computer Use: HSK 3 detail → lesson detail → start → complete → reload vẫn hiển thị “Đã hoàn thành” — pass.

## CURRICULUM-FIX-008 — Lesson detail context

- `GET /api/curriculum/lessons/{id}` trả thêm level Published, topic và unit chứa lesson;
  không thay đổi các field lesson cũ.
- In-memory và Marten store cùng dựng context từ cây Published, nên lesson không thể tự nhận
  nhầm level hoặc lấy context từ dữ liệu Draft.
- Automated: `CurriculumStoreTests` kiểm tra `Level.DisplayName`, `TopicName` và `UnitName`;
  API smoke riêng trên cổng 5057 trả context `HSK 2 / Chủ đề QA / Unit QA` — pass.
