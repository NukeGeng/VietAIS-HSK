# Fixed

## ADMIN-FE-FIX-002 — Giáo trình trong admin shell

- Thêm mục `Giáo trình` lọc theo `curriculum.manage`, đọc
  `/api/admin/curriculum/hsk-levels` để hiển thị cả Draft và Published.
- Nút `Publish` gọi endpoint curriculum riêng, không dùng nhầm Content API.
- Computer Use QA 2026-09-22: import `qa-admin-hsk-3` ở trạng thái Draft → mở admin → thấy
  record `HSK 3 QA` → Publish → notice `Đã publish “HSK 3 QA”` và trạng thái chuyển Published.

## ADMIN-FE-FIX-001 — Admin shell và capability visibility

- Đã tách admin khỏi learner sidebar bằng route `/admin` và shell riêng.
- Navigation chỉ hiển thị module theo capability request-scoped; tài khoản có `content.manage`
  thấy Ngân hàng câu hỏi/Truyện/Video/Tài liệu/Công cụ, còn `users.manage` mới thấy Người dùng.
- Đã kiểm tra bằng Computer Use trên `http://127.0.0.1:5177/admin?qa=admin-shell-20260922`:
  tài khoản `admin-qa` thấy 6 module, publish `story-weekend-draft` hiển thị notice và chuyển
  trạng thái `Draft` → `Published`, sau đó mở danh sách Người dùng thành công.
- Đã kiểm tra capability giới hạn trên
  `http://127.0.0.1:5178/admin?qa=admin-permission-filter-20260922`: tài khoản chỉ có
  `content.manage` không hiển thị mục Người dùng.
- Sửa `/api/me/authorization` để phản ánh quyền từ `IUserContextAccessor` (claims/development
  adapter) thay vì snapshot permission mặc định rỗng; xác nhận API trả đủ
  `content.manage`, `users.manage`, `curriculum.manage`.

## ADMIN-FE-FIX-003 — Audio status/retry

- Thêm mục `Audio` trong admin navigation, chỉ hiện với `content.manage`.
- Hiển thị asset `Ready`/`Failed`/`Pending`, voice, content reference và số lần thử; asset lỗi có
  nút `Thử lại` gọi `POST /api/admin/content/audio/{id}/retry`.
- Computer Use QA 2026-09-22 trên `http://127.0.0.1:5177/admin`: asset lỗi chuyển từ `Failed`
  sang `Pending`, attempt 1 → 2, notice enqueue hiển thị và không clipping ở desktop.
- Curriculum import form/chỉnh sửa sâu và provider audio production vẫn pending theo scope.

## ADMIN-FE-FIX-004 — Import curriculum draft từ Admin shell

- Thêm form content-first trong mục `Giáo trình` cho version, provenance, level metadata và
  `Topics JSON` theo contract backend `Topic → Unit → Lesson`.
- Form chỉ gọi `POST /api/admin/curriculum/import`, hiển thị lỗi validation/idempotency và giữ
  level ở trạng thái Draft để admin review trước khi publish.
- Responsive layout chuyển từ ba cột sang một cột ở mobile, không tạo thêm PageHeader/hero.
- `npm run build` — pass; API import/permission/idempotency đã được kiểm chứng bởi
  `scripts/identity-smoke.mjs`.
- Computer Use QA 2026-09-23 trên
  `http://127.0.0.1:5177/admin?qa=curriculum-import-20260923`: nhập version
  `admin-ui-20260923`, syllabus `HSK 3 Admin UI QA`, level `admin-ui-hsk3-20260923`
  và một topic có unit/lesson; notice `Đã import 1 level vào admin-ui-20260923.` xuất hiện,
  bảng hiển thị record mới ở trạng thái `Draft` cùng nút `Publish`.
