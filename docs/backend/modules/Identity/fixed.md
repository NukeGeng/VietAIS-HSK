# Fixed

## IDENTITY-FIX-006 — Expose request-scoped permissions to admin UI

- `/api/me/authorization` và `/api/me` nay trả `AuthorizationContext` được resolve từ
  `IUserContextAccessor` (trusted claims; development adapter chỉ dùng cho local Computer Use),
  thay vì dùng snapshot permission mặc định rỗng.
- Đã xác nhận bằng API Development riêng trên `5058`: request có
  `X-Dev-Permission: content.manage,users.manage,curriculum.manage` trả đủ ba capability.
- Computer Use xác nhận admin frontend hiển thị đúng module, publish Content và list user; tài
  khoản chỉ có `content.manage` không thấy mục Người dùng.
- Không thay đổi permission policy: các endpoint admin vẫn kiểm tra capability cụ thể.

## IDENTITY-FIX-004 — Validate giờ học tại API

- PreferredStudyTime chỉ chấp nhận null hoặc HH:mm hợp lệ, không chỉ dựa vào input browser.
- 11 test cases mới, toàn bộ suite 71/71 pass.
- HTTP test API riêng 5057: 24:00 → 400, 20:30 → 200; GET /api/me giữ 20:30. Không thay đổi fixture trên API preview 5055.
- Bản API preview đang chạy chưa restart để tránh xóa dữ liệu in-memory.

## IDENTITY-FIX-003 — Bảo toàn avatar khi lưu profile

- ProfileView không gửi null để xóa avatar ngoài ý muốn.
- Computer Use: learner fixture có avatar → reload Hồ sơ → Lưu hồ sơ → hiện “Đã lưu hồ sơ”; GET /api/me xác nhận avatar giữ nguyên. Fixture đã trả về giá trị trước test.
- Input giờ học là time field; build Vue/TypeScript pass.
- Đây là sửa frontend, không thay đổi semantics update profile backend.

## IDENTITY-FIX-001 — Return a consistent forbidden response

### Thay đổi

- Thay `Results.Forbid()` bằng Problem Details HTTP 403 để không phụ thuộc auth provider trong bootstrap Identity.
- Giữ permission check theo capability `users.manage`, không kiểm tra display role.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
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

- `dotnet restore backend/VietAisHsk.Api/VietAisHsk.Api.csproj --ignore-failed-sources` — pass từ package cache.
- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- Full smoke với PostgreSQL ephemeral — pass.
- `scripts/marten-persistence-smoke.mjs`: write process 1 → restart API → read process 2 — pass.

### Computer Use

- Chưa áp dụng; auth provider và production UI chưa triển khai.

### Kết quả

- Profile persistence qua process boundary đã được xác minh bằng PostgreSQL/Marten.
- Các module khác vẫn bootstrap in-memory; chưa tuyên bố toàn hệ thống đã chuyển persistence.

## IDENTITY-FIX-005 — Provisioning/profile ownership regression

### Thay đổi / kiểm chứng

- Thêm unit test xác nhận first-login provisioning idempotent, profile update chỉ thay đổi user hiện tại và admin query chỉ trả user đã provision.
- Computer Use 2026-09-22: learner mở Hồ sơ → sửa display name/múi giờ → nhận `Đã lưu hồ sơ` → reload vẫn giữ dữ liệu — pass.
- Fresh API smoke 2026-09-22: anonymous `/api/me` trả `401`, learner gọi admin trả `403`, admin có capability `users.manage` trả `200`, profile update/reload và target validation pass.

### Giới hạn còn lại

- Auth provider/claim mapping production vẫn là open decision của Identity; `X-Dev-User-Id` chỉ là adapter Development được giới hạn trong `HttpUserContextAccessor`.

## IDENTITY-FIX-007 — Smoke test namespace isolation

- `scripts/identity-smoke.mjs` tạo namespace theo `timestamp/process` cho user, curriculum,
  question và audio fixture do test tạo; các assertion không còn phụ thuộc ID cố định của run trước.
- Assertion draft curriculum chỉ kiểm tra draft hiện tại không xuất hiện trong public list, không
  yêu cầu toàn bộ public catalog phải rỗng.
- Bootstrap extended-content vẫn cần API process sạch khi test publish vì fixture draft được mutate
  có chủ đích; smoke output ghi rõ điều kiện này thay vì che bằng assertion lỏng.
- Verification: `node --check scripts/identity-smoke.mjs`, fresh API smoke Identity và Learning — pass.

## IDENTITY-FIX-008 — Optional JWT/OIDC authentication boundary

### Thay đổi

- Thêm `Microsoft.AspNetCore.Authentication.JwtBearer` và middleware authentication/authorization
  ở composition root, không đưa provider dependency vào business module.
- JWT chỉ bật khi `Authentication:Jwt:Enabled=true` và có đủ `Authority` + `Audience`; cấu hình
  thiếu sẽ làm API fail fast thay vì chạy nửa-authenticated.
- `HttpUserContextAccessor` đọc `sub`/`NameIdentifier` từ trusted principal và các claim
  `permission`/`permissions`; dev header adapter chỉ còn hiệu lực ở Development.
- Không hard-code provider, password flow hay role name; provider/issuer cụ thể vẫn phải được ghi
  trong decision record trước khi deploy.

### Verification

- `dotnet restore VietAisHsk.slnx`, `dotnet build VietAisHsk.slnx --no-restore` — pass, 0 warning.
- `dotnet test VietAisHsk.slnx --no-restore -m:1` — pass `97/97`.
- Unit test xác nhận trusted `sub`, `permission` và `permissions` claims được map ở Production
  context mà không đọc dev headers.
- Cấu hình JWT bật thiếu Authority/Audience được kiểm tra fail-fast; không cho API chạy trong trạng
  thái authentication nửa cấu hình.
- Existing Development identity smoke remains the local adapter regression contract; production
  token validation requires the selected provider's issuer and audience configuration.
