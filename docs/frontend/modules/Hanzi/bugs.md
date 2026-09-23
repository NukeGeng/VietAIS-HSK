# Bugs

## HANZI-FE-BUG-001 — Canvas local không giữ attempt và không có feedback stroke

- Ngày: 2026-09-22
- Mức độ: High
- Flow: learner luyện viết chữ Hán rồi refresh hoặc cần retry một nét.
- Expected: UI gửi attempt/stroke tới Practice, nhận feedback deterministic và hoàn tất được kết quả.
- Actual: canvas chỉ đếm nét local, không có backend attempt.
- Trạng thái: Fixed — xem `HANZI-FE-FIX-002` và backend `PRACTICE-FIX-005`.

## HANZI-FE-FIX-002 — Nối writing canvas với Practice API

- `HanziView` start attempt theo mode, gửi stroke, hiển thị feedback, retry và complete.
- Computer Use desktop flow đã xác nhận sai nét → retry → đủ nét → complete.

## HANZI-FE-BUG-002 — Refresh tạo attempt luyện viết mới

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: learner mở `/app/hanzi/:id/write`, sau đó refresh hoặc component mount lại.
- Expected: tiếp tục attempt đang active của đúng chữ Hán và đúng mode.
- Actual: UI luôn gọi `POST /api/practice/hanzi/:id/attempts`, làm mất liên kết với attempt trước.
- Trạng thái: Fixed — xem `HANZI-FE-FIX-003`.

## HANZI-FE-BUG-003 — Từ liên quan còn render PageView placeholder

- Ngày: 2026-09-23
- Mức độ: Medium
- Flow: learner mở Nền tảng → Chữ Hán → Từ liên quan.
- Expected: route dùng Hanzi catalog hiện tại, hiển thị các nhóm từ liên quan và mở được chữ
  Hán nguồn để xem detail.
- Actual: router dùng `PageView` generic với dữ liệu tĩnh, không phản ánh catalog và không có
  liên kết ngược về chữ Hán.
- Trạng thái: Fixed — xem `HANZI-FE-FIX-005`.
