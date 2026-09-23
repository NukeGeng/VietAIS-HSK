# Integration Tests

Test Marten, Wolverine handler, DB, projection và messaging boundary cần thiết.

## Verified compose flow

`scripts/marten-persistence-smoke.mjs` chạy theo ba pha trên API container có PostgreSQL và
RabbitMQ:

```text
PERSISTENCE_MODE=write
→ ghi profile, curriculum, learning, practice, review và progress
→ lấy session IDs

restart api container

PERSISTENCE_MODE=read
→ đọc lại dữ liệu sau restart, kiểm tra ownership và hoàn tất các flow

PERSISTENCE_MODE=verify
→ replay completion/projection sau khi read phase đã cập nhật state
```

Verification 2026-09-23 trên stack dữ liệu sạch: cả ba pha đều pass. Đây là kiểm chứng
Marten/PostgreSQL thật, không phải chỉ là test store in-memory.

Regression Curriculum 2026-09-23: cùng smoke flow xác nhận import một syllabus version mới với
level Id đã thuộc version cũ trả `409 Conflict`; sau restart, level published vẫn giữ version cũ.
Health checks API và frontend lần lượt trả `200` trên stack QA.
