# Fixed

- ProgressView đã nối overview, weak-points, history và streak với các endpoint Progress tương ứng.
- `PROGRESS-FE-FIX-001` — chuyển tải dữ liệu sang watcher theo route mode, reset state/loading/error trước mỗi request để chuyển giữa bốn trang không giữ dữ liệu cũ.
- `PROGRESS-FE-FIX-002` — item điểm yếu có CTA theo `knowledgeType`: Hanzi mở luyện viết, vocabulary/grammar mở detail, lỗi practice mở hàng đợi `Nội dung cần ôn`.
- Computer Use QA 2026-09-22: shell route `/app/progress` → `/app/progress/weak-points` → `/app/history` → `/app/streak`; mỗi route render đúng vùng nội dung tương ứng sau khi chuyển component — pass.
- Computer Use QA 2026-09-22: `/app/progress/weak-points` hiển thị CTA `Ôn ngay →` trỏ tới `/app/needs-review` — pass.
- Computer Use QA 2026-09-22: weak point `hanzi-writing` hiển thị `Luyện viết →` và mở đúng `/app/hanzi/hanzi-yi/write` — pass.
- `PROGRESS-FE-FIX-003` — question fixture ids (`bootstrap-*`) không còn bị ghép nhầm thành vocabulary/grammar detail; action chuyển về `/app/needs-review` cho đến khi có master-knowledge mapping.
- Computer Use QA 2026-09-22: `bootstrap-vocab-hello` → `Ôn ngay →` mở đúng `/app/needs-review` và queue hiển thị item tương ứng — pass.
- Automated: `npm run build` — pass.
- `PROGRESS-FE-FIX-004` — Progress overview gọi thêm `GET /api/progress/mastery`, hiển thị score/state theo từng nội dung bằng progress bar gọn; có empty state khi chưa có kết quả và grid responsive desktop/mobile.
- Computer Use QA 2026-09-22: `/app/progress` với learner có một practice result sai hiển thị `MỨC ĐỘ NẮM VỮNG`, `Từ vựng 0%`, progress indicator và trạng thái `Cần ôn`; sidebar/route vẫn đầy đủ — pass.
- `PROGRESS-FE-FIX-005` — giữ trạng thái loading/error riêng theo route và hiển thị empty state có ngữ cảnh khi learner chưa có dữ liệu tiến độ; không tạo khoảng trống hoặc dashboard giả.
- Computer Use QA 2026-09-22 ở viewport 390×844: `/app/progress` hiển thị đủ ba metric `Đã trả lời 0`, `Đúng 0`, `Cần ôn 0` và vùng `MỨC ĐỘ NẮM VỮNG` với thông báo `Chưa có kết quả đủ để tính mức độ nắm vững`; không clipping/overflow — pass.
