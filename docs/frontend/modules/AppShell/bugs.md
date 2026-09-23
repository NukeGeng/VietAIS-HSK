# Bugs

- `APPSHELL-BUG-001` — Bản kiểm tra đầu tiên làm tiêu đề dashboard bị dính chữ vì `letter-spacing` âm quá mạnh.
- `APPSHELL-BUG-002` — Parent route `/app` bị active trên mọi route con vì kiểm tra prefix không loại trừ route gốc.
- `APPSHELL-BUG-003` — Nhiều màn learner lặp tiêu đề/mô tả trong intro lớn bên dưới breadcrumb; route generic còn hiện nhầm topbar “Tổng quan” vì thiếu `meta.title`.
- `APPSHELL-BUG-004` — CTA beginner bị xuống dòng trên mobile; mũi tên trong metadata của hàng học bị tách khỏi nhãn ở cả desktop lẫn mobile.
- `APPSHELL-BUG-005` — `expandedMenus` khởi tạo `false` có thể che submenu dù route con đang active; sau khi chuyển trang, sidebar làm mất các nhãn kỹ năng/luyện tập.
- `APPSHELL-BUG-006` — Production vẫn coi `/app/skills` và `/app/practice` là page độc lập; dashboard còn trỏ vào overview Kỹ năng dư, trái với navigation rule parent chỉ là dropdown.
- `APPSHELL-BUG-007` — Biểu tượng cấp độ trên sidebar bị hardcode `3`, nên khi learner chọn cấp khác nhãn hiển thị không khớp với dữ liệu curriculum.
