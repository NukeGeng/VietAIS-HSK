# Fixed

## CURRICULUM-FIX-001 — Bootstrap versioned curriculum read/import flow

### Thay đổi

- Thêm model `SyllabusVersion`, `HskLevel`, beginner stages và foundation catalogs.
- Thêm import validation cho version, provenance, duplicate key/level và level range 1–9.
- Import ở trạng thái Draft; public query chỉ trả nội dung Published.
- Thêm publish flow yêu cầu capability `curriculum.manage`.
- Giữ HSK data thật ngoài code cho tới khi có dataset/provenance đã được duyệt.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
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

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- Marten smoke: import/publish trước restart, `hsk-levels` sau restart — pass.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Curriculum version/level đã được kiểm tra persistence qua process boundary.
- Topic/unit/lesson và master data chi tiết vẫn chờ HSK dataset/provenance được đưa vào import pipeline.
