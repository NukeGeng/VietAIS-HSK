# Docker

## Local compose

`compose.yaml` ở repository root là deployment contract có thể chạy được cho local/staging-like:

```text
postgres  -> 5432
rabbitmq  -> 5672 / management 15672
api       -> 5055 (/health)
frontend  -> 5173 (Nginx, proxy /api tới api:8080)
```

Chạy:

```bash
docker compose up --build
```

Khi local đang dùng các cổng mặc định, có thể chạy một stack kiểm chứng song song bằng cách
đổi `POSTGRES_PORT`, `RABBITMQ_PORT`, `RABBITMQ_MANAGEMENT_PORT`, `API_PORT` và `FRONTEND_PORT`.

Postgres và RabbitMQ phải healthy trước khi API khởi động. API có healthcheck gọi `/health`, còn
Nginx frontend proxy healthcheck qua `/health`; frontend chỉ start sau khi API healthy. Compose dùng
volume có tên để dữ liệu local không biến mất khi container được recreate; production cần
volume/backup policy riêng.

Dockerfile API nằm tại `deploy/Dockerfile.api`, frontend tại `deploy/Dockerfile.frontend`.

## Verification

Ngày 2026-09-23, một stack compose tách biệt đã được dựng bằng cổng host thay thế để không
đụng các service local khác:

```text
PostgreSQL 55433 (healthy)
RabbitMQ 55674 / management 55675 (healthy)
API 6056 (container healthy, `/health -> 200`)
Frontend 6174 (container healthy, `/health -> 200`, `/app/skills/listening -> 200`)
```

Image API/frontend build thành công. `identity-smoke.mjs` và
`learning-completion-smoke.mjs` đều pass khi chạy qua API container có PostgreSQL + RabbitMQ.
Stack kiểm chứng này không dùng để thay thế staging; credentials trong compose chỉ là local-only.
