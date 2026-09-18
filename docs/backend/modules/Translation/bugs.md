# Bugs

Chưa ghi nhận bug.

## TRANSLATION-BUG-002 — Translation response có nguy cơ lộ reference answer

- Ngày: 2026-09-18
- Mức độ: High
- Flow: exercise catalog/attempt/history.
- Expected: reference Chinese chỉ dùng trong feedback domain/provider.
- Actual: domain exercise/attempt có thể serialize `referenceChinese`.
- Trạng thái: Fixed — xem `TRANSLATION-FIX-002`.
