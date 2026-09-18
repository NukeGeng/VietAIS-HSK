# Fixed

## PRACTICE-FIX-001 — Deterministic session and grading bootstrap

### Thay đổi

- Thêm practice session, question attempt, deterministic accepted-answer grading và session result.
- Session/query/answer/complete đều lấy `UserId` từ trusted context và không nhận user ID từ client.
- Complete session idempotent; truy cập session của user khác trả 404.
- Chuẩn hóa enum response thành chuỗi JSON (`Correct`, `Incorrect`, `NeedsRetry`).
- Giữ question catalog fixture tách qua reader contract; dữ liệu HSK chính thức vẫn thuộc Content.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm auth, grading đúng/sai, isolation và complete idempotency.

### Computer Use

- Chưa áp dụng; production frontend và Hanzi writing canvas chưa triển khai.

### Kết quả

- Practice grading không gọi AI và không dùng RabbitMQ.
- API trả lỗi rõ ràng cho session không tồn tại, answer thiếu và session đã hoàn thành.
