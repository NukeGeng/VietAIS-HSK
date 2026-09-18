# Fixed

## IDENTITY-FIX-001 — Return a consistent forbidden response

### Thay đổi

- Thay `Results.Forbid()` bằng Problem Details HTTP 403 để không phụ thuộc auth provider trong bootstrap Identity.
- Giữ permission check theo capability `users.manage`, không kiểm tra display role.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass.

### Computer Use

- Chưa áp dụng ở bootstrap backend-only; UI flow sẽ được kiểm thử khi production frontend được triển khai từ `design-template`.

### Kết quả

- Anonymous `/api/me`: 401.
- Learner admin access: 403.
- Admin có `users.manage`: 200.
- Profile update/reload và validation HSK reference: pass.

## IDENTITY-FIX-002 — Marten-backed Identity persistence

### Thay đổi

- Thêm `MartenIdentityStore` với `IdentityDocument` và concrete `HashSet<string>` cho persisted permissions.
- Bật Marten opt-in qua `ConnectionStrings:Postgres`; không có connection string thì giữ bootstrap in-memory cho local smoke.
- Admin search dùng `ToListAsync()` theo yêu cầu Marten 9.
- Giữ auth provider là open decision; dev header adapter chỉ tồn tại cho local Computer Use/smoke flow.

### Automated test

- `dotnet restore src/VietAisHsk.Api/VietAisHsk.Api.csproj --ignore-failed-sources` — pass từ package cache.
- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- Full smoke với PostgreSQL ephemeral — pass.
- `scripts/marten-persistence-smoke.mjs`: write process 1 → restart API → read process 2 — pass.

### Computer Use

- Chưa áp dụng; auth provider và production UI chưa triển khai.

### Kết quả

- Profile persistence qua process boundary đã được xác minh bằng PostgreSQL/Marten.
- Các module khác vẫn bootstrap in-memory; chưa tuyên bố toàn hệ thống đã chuyển persistence.
