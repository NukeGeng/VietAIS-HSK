# Bugs

## IDENTITY-BUG-004 — Giờ học không được validate

- ValidateProfile chỉ kiểm tra DailyMinutes, cho phép PreferredStudyTime như `24:00` hoặc chuỗi bất kỳ được lưu.
- Sửa bằng TimeOnly.TryParseExact HH:mm, invariant culture; null vẫn hợp lệ khi chưa chọn giờ.

## IDENTITY-BUG-003 — Form hồ sơ xóa avatar khi lưu

- Frontend gửi avatarUrl null trong mọi lần lưu dù hồ sơ đã có avatar.
- Đã sửa ProfileView gửi avatarUrl từ snapshot hiện tại; input giờ học dùng type=time.
- Verification: xem IDENTITY-FIX-003.

## IDENTITY-BUG-001 — Unauthorized admin response caused a server error

- Ngày: 2026-09-18
- Mức độ: Medium
- Flow: learner gọi `GET /api/admin/users` không có `users.manage`.
- Expected: HTTP 403 theo nhóm lỗi forbidden.
- Actual: HTTP 500 vì `Results.Forbid()` yêu cầu authentication service chưa được cấu hình khi auth provider còn là open decision.
- Trạng thái: Fixed — xem `IDENTITY-FIX-001`.

## IDENTITY-BUG-002 — Marten document and query compatibility

- Ngày: 2026-09-18
- Mức độ: Medium
- Flow: chạy API với `ConnectionStrings:Postgres` và thực hiện profile/admin user flow.
- Actual 1: `IReadOnlySet<string>` không materialize được từ Marten JSON document.
- Actual 2: synchronous LINQ query bị Marten 9 từ chối.
- Trạng thái: Fixed — xem `IDENTITY-FIX-002`.

## IDENTITY-BUG-005 — Production chưa có authentication middleware cấu hình được

- Trước fix, Identity chỉ đọc trusted claims nếu host/provider tự đăng ký ở ngoài; API chưa có
  boundary JWT/OIDC chuẩn để deployment cấu hình mà không sửa business module.
- Rủi ro: production có thể khởi động nhưng mọi request learner đều anonymous hoặc provider
  integration bị triển khai ad-hoc.
- Trạng thái: Fixed ở mức provider-agnostic adapter — xem `IDENTITY-FIX-008`. Provider, issuer và
  claim mapping cụ thể vẫn là open decision.
