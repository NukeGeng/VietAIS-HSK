# Bugs

## EXAM-BUG-006 — Subjective grading callback chỉ có dev permission boundary

- Ngày: 2026-09-23
- Mức độ: High
- Flow: AI grading worker gửi kết quả về `/api/exam-attempts/{id}/subjective-grading`.
- Expected: production callback được xác thực độc lập với learner auth, có chống replay theo
  timestamp và vẫn giữ correlation/idempotency của `JobId`.
- Actual: endpoint chỉ chấp nhận `X-Dev-Permission` trong Development; chưa có transport auth
  cho worker thật.
- Trạng thái: Fixed ở callback boundary — xem `EXAM-FIX-009`; worker/provider/deployment thật
  vẫn là production follow-up.

Các mục bên dưới là lịch sử bug đã được xử lý.

## EXAM-BUG-007 — Failed subjective result làm mất feedback khi rebuild attempt

- Ngày: 2026-09-23
- Mức độ: Medium
- Flow: worker callback `Status=Failed` → đọc lại ExamAttempt.
- Expected: learner/admin thấy lý do provider thất bại để biết có thể thử lại hoặc chờ xử lý.
- Actual: state đã chuyển `Failed` nhưng projection chỉ lấy feedback từ event Completed, nên
  `SubjectiveFeedback` bị null sau callback.
- Trạng thái: Fixed — xem `EXAM-FIX-010`.

## EXAM-BUG-002 — Exam response có nguy cơ lộ accepted answers/event stream

- Ngày: 2026-09-18
- Mức độ: High
- Flow: catalog/start/resume exam.
- Expected: learner không nhận answer key hoặc event stream nội bộ.
- Actual: domain attempt có cả accepted answers và events.
- Trạng thái: Fixed — xem `EXAM-FIX-002`.

## EXAM-BUG-003 — Attempt chưa có persistence khi chạy với Postgres/Marten

- Ngày: 2026-09-22
- Mức độ: High
- Flow: start/resume/submit exam trong môi trường production.
- Expected: attempt được rebuild từ event stream sau request hoặc process restart.
- Actual: implementation trước đó chỉ có `InMemoryExamStore`, nên dữ liệu mất khi
  process restart và không đáp ứng event-sourcing contract.
- Trạng thái: Fixed — xem `EXAM-FIX-003`.

## EXAM-BUG-004 — Frontend không có route detail/resume/result tách biệt

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: catalog → detail → attempt → result.
- Expected: route transition giữ đúng trạng thái và refresh attempt vẫn đọc được.
- Actual: view chỉ mount một lần và không phản ứng khi route params đổi, khiến
  catalog có thể còn hiển thị sau khi chuyển sang detail.
- Trạng thái: Fixed — xem frontend `EXAM-FE-FIX-001`.

## EXAM-BUG-005 — Subjective attempt chưa biểu diễn trạng thái chờ chấm

- Ngày: 2026-09-23
- Mức độ: Medium
- Flow: submit exam có câu tự luận.
- Expected: objective result trả ngay, phần tự luận có trạng thái `Pending` và callback
  phải correlation/idempotent.
- Actual: contract chỉ có objective score; chưa có request/result event và state để frontend
  phân biệt đang chờ chấm.
- Trạng thái: Fixed ở local/test adapter — xem `EXAM-FIX-006`; provider AI production vẫn pending.
