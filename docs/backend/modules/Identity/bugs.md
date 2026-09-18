# Bugs

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
