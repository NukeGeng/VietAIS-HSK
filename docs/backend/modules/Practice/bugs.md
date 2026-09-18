# Bugs

## PRACTICE-BUG-001 — Grading enum response was serialized as a number

- Ngày: 2026-09-18
- Mức độ: Medium
- Flow: submit answer trong practice session.
- Expected: client nhận được kết quả đọc được như `Correct` hoặc `Incorrect`.
- Actual: JSON mặc định serialize enum thành số.
- Trạng thái: Fixed — xem `PRACTICE-FIX-001`.
