# Fixed

## EXAM-FE-FIX-003 — Mobile focus layout

- Computer Use QA ở 390×844: danh sách đề, chi tiết đề và màn làm bài đều hiển thị trong content width; question navigation, input và Previous/Next không clipping.
- Flow đã kiểm tra: danh sách → chi tiết → bắt đầu đề; trạng thái `Active` và 0/2 câu đã lưu hiển thị đúng.
- Chưa đánh dấu responsive cho mọi route phụ ngoài flow này.

- `ExamView` đã nối catalog → detail → start attempt → submit answer → submit →
  result bằng các route riêng.
- Route-aware watcher tải lại dữ liệu khi `examId` hoặc `attemptId` đổi, không còn
  phụ thuộc vào việc component được mount mới.
- Attempt page đọc lại answers từ API sau reload; result page hiển thị objective
  score và số câu đúng.
- Computer Use flow: mở đề bootstrap, trả lời hai câu, reload để kiểm tra resume,
  nộp và hiển thị `100 / 100` — pass.
- Computer Use QA 2026-09-22: danh sách → chi tiết → bắt đầu attempt → trả lời `xin chào`/`cảm ơn` → refresh vẫn giữ `2/2` → nộp hiển thị `100 / 100`; mở lại attempt đã nộp chỉ hiển thị result, không tạo double-submit — pass.
- `npm run build` — pass.

## EXAM-FE-FIX-001 — Route-aware exam navigation

- Thay `onMounted` đơn lẻ bằng `watch` trên route props với `immediate: true`.
- Tách rõ catalog/detail/attempt/result state và loading/error state.
- Không thêm PageHeader/hero blocking; giữ content-first layout theo UI layout rules.

## EXAM-FE-FIX-002 — Question navigation và submit confirmation

### Thay đổi

- Màn attempt chỉ tập trung một câu tại một thời điểm, có question navigator, `Câu trước` và `Câu tiếp theo`.
- Answer được lưu qua API khi chuyển câu hoặc trước khi xác nhận nộp.
- Nút cuối mở confirm panel; learner có thể quay lại hoặc xác nhận nộp rõ ràng.

### Verification

- Computer Use QA 2026-09-22: start bootstrap exam → nhập câu 1 → chuyển câu 2 → nhập câu 2 → mở `Xác nhận nộp bài?` → xác nhận → `100 / 100` — pass.
- Computer Use retest: với câu cuối chỉ đang ở draft, confirm hiển thị `Bạn đã trả lời tất cả câu hỏi.` trước khi submit — pass.
- `npm run build` — pass.

### Chưa hoàn tất

- Mobile/focus visual regression 390px vẫn pending.

## EXAM-FE-FIX-004 — Subjective grading pending state

- Mở rộng public attempt/result types với `subjectiveGradingStatus`, `subjectiveScore` và
  `subjectiveFeedback`; câu hỏi chỉ expose `questionType`, không expose answer key.
- Result hiển thị rõ `Phần tự luận đang chờ chấm`, điểm/feedback khi `Completed` và thông báo
  retry khi `Failed`; objective score vẫn được hiển thị độc lập.
- Computer Use QA 2026-09-23: danh sách đề → mở `Bài thi viết HSK 3 bootstrap` → nhập câu
  `我叫小明。` → nộp → result hiển thị `0 / 100` và `Phần tự luận đang chờ chấm` — pass.
- `npm run build` — pass.
- Mobile/focus visual regression 390px cho state này vẫn pending.
