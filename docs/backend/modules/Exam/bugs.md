# Bugs

Chưa ghi nhận bug.

## EXAM-BUG-002 — Exam response có nguy cơ lộ accepted answers/event stream

- Ngày: 2026-09-18
- Mức độ: High
- Flow: catalog/start/resume exam.
- Expected: learner không nhận answer key hoặc event stream nội bộ.
- Actual: domain attempt có cả accepted answers và events.
- Trạng thái: Fixed — xem `EXAM-FIX-002`.
