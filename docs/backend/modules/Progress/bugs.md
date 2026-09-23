# Bugs

## PROGRESS-BUG-001 — Progress snapshot mất khi API restart

- Ngày: 2026-09-22
- Mức độ: High
- Flow: practice result hoặc learning activity cập nhật progress, sau đó API restart.
- Expected: snapshot, điểm yếu và history được đọc lại từ persistence; chỉ learner sở hữu dữ liệu mới xem được.
- Actual: `InMemoryProgressStore` là store duy nhất, nên toàn bộ projection mất theo process.
- Trạng thái: Fixed — xem `PROGRESS-FIX-002`; PostgreSQL smoke kiểm tra qua hai lần restart.

## PROGRESS-BUG-002 — Retry answer bị tính thành nhiều practice result

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: client gửi lại answer cho cùng câu hỏi trong cùng practice session.
- Expected: projection phản ánh attempt hiện tại của `(session, question)`, không cộng thêm result trùng.
- Actual: sink cộng bộ đếm mỗi lần nhận signal và không có event identity.
- Trạng thái: Fixed — xem `PROGRESS-FIX-002`; event key ổn định theo session/question và smoke gửi lại cùng answer.

## PROGRESS-BUG-003 — Persistence smoke gộp nhầm history của hai learner

- Ngày: 2026-09-23
- Mức độ: Medium (QA harness)
- Flow: smoke test dùng một learner cho learning và một learner cho practice/review, nhưng pha read/verify đọc `/api/progress/history` chỉ với header của learner practice/review rồi lại yêu cầu có cả `lesson-completed`.
- Expected: mỗi history được kiểm tra với đúng identity sở hữu dữ liệu; activity không được xuất hiện chéo giữa hai learner.
- Actual: harness báo `Learning/Practice/Review completion history missing` hoặc yêu cầu `completedActivities >= 3` dù Marten document và API vẫn lưu/đọc đúng dữ liệu theo user.
- Trạng thái: Fixed — xem `PROGRESS-FIX-008`; smoke tách hai assertion theo `learningHeaders` và `headers`.
