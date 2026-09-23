# Bugs

- `PROGRESS-FE-BUG-001` — `ProgressView` chỉ gọi API trong `onMounted`; khi chuyển giữa các route overview/weak-points/history/streak bằng cùng component, nội dung không được tải lại theo mode mới.

- `PROGRESS-FE-BUG-002` — weak point từ practice dùng id `bootstrap-*` nhưng UI ghép thành route detail vocabulary/grammar không tồn tại trong master catalog.
  - Trạng thái: Fixed — question fixture chuyển về `/app/needs-review`; master id vẫn mở detail.
