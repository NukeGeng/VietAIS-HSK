# Bugs

## EXPLORE-FE-BUG-002 — Detail truyện lặp header

- QA mobile 390px: sau app bar còn header “Chi tiết truyện”, hai dòng “MỞ RỘNG · TRUYỆN”, rồi mới tới tiêu đề truyện.
- Sửa: khi có detail, bỏ header tổng quát và kicker lặp; giữ link quay lại cùng tiêu đề nội dung.

## EXPLORE-FE-BUG-001 — API lỗi bị thay bằng dữ liệu mẫu

- PageView catch lỗi API rồi hiện các item tĩnh của template, kể cả detail không tồn tại.
- Response chậm của route cũ còn có thể ghi đè nội dung route mới.
- Sửa: error state có Thử lại, không fallback dữ liệu mẫu; chỉ request mới nhất được cập nhật state.
