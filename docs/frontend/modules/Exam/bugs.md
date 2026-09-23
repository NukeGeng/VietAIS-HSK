# Bugs

Chưa ghi nhận bug.

## EXAM-FE-BUG-001 — Route reuse giữ lại catalog sau khi vào detail

- Ngày: 2026-09-22
- Mức độ: Medium
- Expected: khi `examId` đổi, view tải definition/attempt tương ứng.
- Actual: `onMounted` không chạy lại khi Vue reuse cùng component cho route mới,
  nên detail có thể tiếp tục hiển thị catalog cũ.
- Trạng thái: Fixed — xem `EXAM-FE-FIX-001`.

## EXAM-FE-BUG-002 — Submit confirm đếm thiếu draft hiện tại

- Ngày: 2026-09-22
- Mức độ: Low
- Flow: nhập câu cuối nhưng chưa blur/save → mở xác nhận nộp.
- Actual: confirm báo còn một câu chưa trả lời dù draft đã có nội dung.
- Trạng thái: Fixed — bộ đếm tính cả draft hiện tại trước khi gửi API.
