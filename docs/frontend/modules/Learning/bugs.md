# Bugs

- `LEARNING-FE-BUG-001` — Khi backend chưa có dữ liệu hoặc chưa có phiên xác thực, Home cần hiển thị trạng thái lỗi có hướng dẫn thay vì vỡ layout. **Fixed — LEARNING-FE-FIX-001.**
- `LEARNING-FE-BUG-002` — Route HSK level và lesson detail còn rơi vào PageView placeholder, không nối được cây Topic → Unit → Lesson với start/complete state. **Fixed — LEARNING-FE-FIX-005.**
- `LEARNING-FE-BUG-003` — Trang danh sách `/app/lessons` dùng dữ liệu tĩnh trong PageView thay vì published Curriculum API. **Fixed — LEARNING-FE-FIX-006.**
- `LEARNING-FE-BUG-004` — `LearningPathView` chỉ tải trong `onMounted`; chuyển giữa `/app/hsk`, `/app/hsk/:level` và `/app/beginner` bằng cùng component có thể giữ dữ liệu của route trước. **Fixed — LEARNING-FE-FIX-008.**
- `LEARNING-FE-BUG-005` — FoundationView không hiển thị hoặc cập nhật trạng thái hoàn tất Beginner stage; người học chỉ xem được dữ liệu nền tảng mà không nối được journey. **Fixed — LEARNING-FE-FIX-010.**

- `LEARNING-FE-BUG-006` — LearningPathView không đọc kết quả completion unit/level từ HSK learning response; lesson đã hoàn thành vẫn hiển thị như chưa học và không có tổng kết cấp độ. **Fixed — LEARNING-FE-FIX-012.**
- `LEARNING-FE-BUG-007` — HomeView lấy nhãn `HSK 3` cố định cho section Kỹ năng, có thể lệch với cấp độ người học đang chọn từ Curriculum/Identity. **Fixed — LEARNING-FE-FIX-015.**
- `LEARNING-FE-BUG-008` — `LessonDetailView` chỉ tải trong `onMounted`; chuyển trực tiếp giữa hai lesson detail có thể giữ dữ liệu lesson trước vì Vue reuse component theo cùng route record. **Fixed — LEARNING-FE-FIX-016.**
- `LEARNING-FE-BUG-009` — Lesson detail hiển thị nhãn `HSK 3` hardcode dù lesson thuộc level khác, làm context detail lệch với curriculum/sidebar. **Fixed — LEARNING-FE-FIX-017.**
