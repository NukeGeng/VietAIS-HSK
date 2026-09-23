# Workers Deployment

## Subjective grading worker

`backend/VietAisHsk.Workers` là process worker riêng, không đặt consumer vào API process.
Worker dùng Wolverine/RabbitMQ để consume queue `vietais.exam.subjective-grading`, dùng
PostgreSQL-backed durable inbox, rồi gọi callback HMAC vào API. `JobId`/Exam store vẫn là
correlation và idempotency boundary của kết quả.

Chạy local/staging-like cùng compose:

```bash
WORKER_CALLBACK_SHARED_SECRET='local-worker-secret' \
docker compose --profile workers up --build
```

Kiểm tra flow end-to-end khi API container đang chạy:

```bash
IDENTITY_API_URL=http://127.0.0.1:5055 node scripts/worker-smoke.mjs
```

Provider mặc định là `UnconfiguredSubjectiveGradingProvider`: worker fail-closed và lưu trạng
thái `Failed` qua callback đã ký. Đây là behavior an toàn khi chưa có quyết định/provider AI thật;
không được coi là DeepSeek grading đã triển khai.

## Còn pending

- adapter/provider AI đã được duyệt và structured-output validation;
- CosyVoice audio consumer và object storage thật;
- retry/dead-letter policy vận hành, metrics/alerting, secret manager/TLS;
- scaling/restart/rollback policy trên staging.
