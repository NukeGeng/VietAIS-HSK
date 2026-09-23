# Bugs

Chưa ghi nhận bug.

## TRANSLATION-BUG-001 — Feedback provider failure chưa phân loại

- Ngày: 2026-09-22
- Flow: learner đã lưu attempt rồi yêu cầu góp ý.
- Actual: lỗi provider/timeout có thể đi qua cùng một thông báo generic và contract feedback chưa được validate trước khi lưu.
- Trạng thái: Fixed — xem `TRANSLATION-FIX-004`.

## TRANSLATION-BUG-002 — Translation response có nguy cơ lộ reference answer

- Ngày: 2026-09-18
- Mức độ: High
- Flow: exercise catalog/attempt/history.
- Expected: reference Chinese chỉ dùng trong feedback domain/provider.
- Actual: domain exercise/attempt có thể serialize `referenceChinese`.
- Trạng thái: Fixed — xem `TRANSLATION-FIX-002`.
