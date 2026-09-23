# Fixed

- ReviewView đã nối summary, mistakes và needs-review với loading/error/empty state.
- `REVIEW-FE-FIX-002` — ReviewView tạo session từ queue hiện tại, ghi kết quả từng item, chuyển item kế tiếp và hiển thị completion state.
- Computer Use: Practice wrong → `/app/review` → bắt đầu ôn → ghi kết quả — pass trên local Vite/API.
- Computer Use 2026-09-22: Practice listening trả lời sai → complete `0/1` → `/app/needs-review` hiển thị `bootstrap-listening-classroom` — pass.
- `REVIEW-FE-FIX-003` — ReviewView watcher theo `mode`, reset summary/items/session/loading trước mỗi request để ba route review chuyển đổi đúng state.
- Computer Use QA 2026-09-22: `/app/review` → `/app/mistakes` → `/app/needs-review`; route đổi và danh sách tương ứng được tải lại — pass.
- `REVIEW-FE-FIX-004` — Thêm filter loại nội dung cho `Nội dung cần ôn`, action mở context tương ứng và route `/app/hanzi/{id}/write` cho item Hanzi writing; bootstrap question chuyển về practice context thay vì catalog id giả.
- Computer Use QA 2026-09-22: Practice wrong → `/app/needs-review` hiển thị filter `vocabulary`, item `bootstrap-vocab-hello` mở `/app/practice/vocabulary`; `/app/review` hiển thị summary, start session, record result và completion state — pass.
- Computer Use QA 2026-09-22: tạo `WritingWeak` từ lượt Hanzi không đạt → `/app/needs-review` hiển thị `hanzi-writing · hanzi-da` → `Mở nội dung` mở đúng `/app/hanzi/hanzi-da/write` và reset lượt viết mới — pass.

### Chưa hoàn tất

- Regression mobile toàn bộ các route detail khác vẫn cần chạy riêng; flow Hanzi writing đã được QA mobile trong `docs/frontend/modules/Hanzi/fixed.md`.
