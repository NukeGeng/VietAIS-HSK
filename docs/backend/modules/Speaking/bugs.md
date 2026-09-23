# Bugs

## SPEAKING-BUG-001 — Provider exception/timeout làm mất trạng thái lượt nói

- Ngày: 2026-09-22
- Flow: learner gửi một lượt nói khi provider phản hồi lỗi hoặc quá lâu.
- Actual: endpoint await trực tiếp provider; exception có thể thành 500 và turn không được ghi nhận.
- Trạng thái: Fixed — xem `SPEAKING-FIX-003`.

## SPEAKING-BUG-002 — Session id không được kiểm tra theo owner ở boundary

- Ngày: 2026-09-22
- Flow: learner dùng session id của learner khác.
- Expected: không đọc/ghi/kết thúc được session.
- Trạng thái: Fixed — xem `SPEAKING-FIX-003`; smoke test xác nhận 404.
