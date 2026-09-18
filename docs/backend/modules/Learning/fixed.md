# Fixed

## LEARNING-FIX-001 — Bootstrap learner path state

### Thay đổi

- Thêm learner state cho beginner/HSK context, current stage/lesson và started/completed lesson IDs.
- Thêm `GET /api/learning/home`, beginner start/view, HSK select/view và lesson start/complete routes.
- Beginner start idempotent và bắt đầu ở stage `pinyin`.
- Lesson chỉ được mutate khi Curriculum xác nhận lesson đã Published.
- Complete lesson idempotent; chưa start thì trả business conflict.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm beginner start, continue target, HSK selection và unpublished lesson guard.

### Computer Use

- Chưa áp dụng; production frontend chưa được triển khai.

### Kết quả

- Learning state không nhận `UserId` từ request body; lấy từ trusted user context.
- Beginner và HSK context không dùng AI/RabbitMQ.
- Chưa đánh dấu module hoàn tất vì storage hiện là bootstrap in-memory và chưa có event replay/Marten persistence.
