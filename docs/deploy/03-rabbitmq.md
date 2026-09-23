# RabbitMQ

RabbitMQ chỉ phục vụ các job async đã được xác định trong Goal: audio generation và
subjective exam grading. Không route CRUD, review/progress signal hoặc Speaking per-turn qua
broker.

## Local configuration

API chỉ bật transport khi có đủ ba điều kiện:

```text
Messaging__EnableWolverine=true
ConnectionStrings__Postgres=<postgres connection string>
ConnectionStrings__RabbitMq=amqp://<user>:<password>@<host>:5672/<vhost>
```

Khi thiếu một điều kiện, API giữ in-memory queue fallback để local UI/smoke test không bị
phụ thuộc RabbitMQ.

## Queues

```text
vietais.audio.generate
vietais.exam.subjective-grading
```

Wolverine khai báo queue qua `AutoProvision()` và bật durable inbox/outbox policy cho endpoint
external. Worker phải xử lý idempotent theo `IdempotencyKey`/`JobId`, retry có giới hạn và
đưa lỗi không xử lý được vào dead-letter flow.

## Production pending

- worker process cho CosyVoice và AI grading;
- credentials/TLS/vhost thật;
- retry/dead-letter retention và alert;
- health check broker trong deployment.

AI grading callback boundary đã có HMAC signature contract; phần còn pending là worker consumer,
secret manager/TLS provisioning, retry/dead-letter operational policy và deployment health check.
