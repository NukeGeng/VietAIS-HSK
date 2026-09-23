# Bugs

## PRACTICE-BUG-001 — Grading enum response was serialized as a number

- Ngày: 2026-09-18
- Mức độ: Medium
- Flow: submit answer trong practice session.
- Expected: client nhận được kết quả đọc được như `Correct` hoặc `Incorrect`.
- Actual: JSON mặc định serialize enum thành số.
- Trạng thái: Fixed — xem `PRACTICE-FIX-001`.

## PRACTICE-BUG-002 — Practice response có nguy cơ lộ accepted answers

- Ngày: 2026-09-18
- Mức độ: High
- Flow: tạo/lấy practice session.
- Expected: learner chỉ nhận prompt và trạng thái câu hỏi.
- Actual: domain question có thể serialize `acceptedAnswers` ra response.
- Trạng thái: Fixed — xem `PRACTICE-FIX-002`.

## PRACTICE-BUG-003 — Active practice session mất khi API restart

- Ngày: 2026-09-22
- Mức độ: High
- Flow: learner tạo session, trả lời một câu rồi refresh/khởi động lại API.
- Expected: session, answer và trạng thái Active/Completed vẫn đọc được; learner khác không truy cập được.
- Actual: `InMemoryPracticeStore` là store duy nhất được đăng ký, nên process restart xóa session.
- Trạng thái: Fixed — xem `PRACTICE-FIX-003`; Marten smoke đã kiểm tra active session và completion qua hai lần restart.

## PRACTICE-BUG-004 — Hanzi writing chỉ có canvas local, chưa có validator/attempt

- Ngày: 2026-09-22
- Mức độ: High
- Flow: learner mở Chữ Hán → luyện viết → sai nét → retry → hoàn tất → Ôn tập/Tiến độ.
- Expected: attempt lưu theo user, stroke dataset version được chốt, validator deterministic trả feedback và kết quả gửi signal sang Review/Progress.
- Actual: frontend chỉ đếm nét trên canvas; refresh làm mất state và không có API attempt.
- Trạng thái: Fixed — xem `PRACTICE-FIX-005`; PostgreSQL/Marten persistence và full geometry benchmark vẫn cần kiểm thử thêm khi dataset chính thức được import.

## PRACTICE-BUG-005 — Skill routes không tạo practice session theo kỹ năng

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: learner mở Kỹ năng → Nghe/Đọc/Viết → bắt đầu luyện.
- Expected: route đi vào session deterministic với question thuộc đúng skill.
- Actual: route dùng PageView mô tả; question catalog không có filter theo skill.
- Trạng thái: Fixed — xem `PRACTICE-FIX-006`; question hiện là platform-authored fixture, chưa phải
  question bank chính thức.
