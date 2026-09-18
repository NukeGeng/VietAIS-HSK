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
