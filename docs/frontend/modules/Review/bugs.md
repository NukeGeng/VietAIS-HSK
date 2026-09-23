# Bugs

## REVIEW-FE-BUG-001 — Review routes chỉ render danh sách tĩnh

- Ngày: 2026-09-22
- Flow: learner mở Ôn tập/Câu làm sai/Nội dung cần ôn.
- Expected: có thể bắt đầu phiên ôn và ghi kết quả cho item.
- Actual: UI chỉ hiển thị dữ liệu đọc được, chưa có session action.
- Trạng thái: Fixed — xem `REVIEW-FE-FIX-002`.
## REVIEW-FE-BUG-002 — ReviewView không reset khi đổi mode

- Ngày: 2026-09-22
- Flow: chuyển giữa summary/mistakes/needs-review trong cùng component.
- Actual: queue của route trước có thể còn hiển thị do chỉ tải dữ liệu ở `onMounted`.
- Trạng thái: Fixed — xem `REVIEW-FE-FIX-003`.

## REVIEW-FE-BUG-003 — Review item bootstrap trỏ sai detail route

- Ngày: 2026-09-22
- Flow: Practice tạo item `bootstrap-vocab-hello` → Nội dung cần ôn → Mở nội dung.
- Actual: dùng question id như vocabulary id khiến detail catalog không tìm thấy record.
- Trạng thái: Fixed — item bootstrap chuyển về `/app/practice/vocabulary`; knowledge id thật vẫn mở detail catalog.
