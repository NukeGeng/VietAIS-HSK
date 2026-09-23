# Backend Deployment

## Container contract

API chạy từ `deploy/Dockerfile.api` và lắng nghe `http://+:8080` trong container. Các biến bắt
buộc của stack:

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__Postgres
ConnectionStrings__RabbitMq
Messaging__EnableWolverine
Authentication__Jwt__Enabled
Authentication__Jwt__Authority
Authentication__Jwt__Audience
Authentication__Jwt__RequireHttpsMetadata
```

`GET /health` là liveness check. Image API cài `curl` tối thiểu chỉ để Docker healthcheck gọi
endpoint này; không dùng nó cho business request. Readiness của local compose được đảm bảo bằng
healthcheck của Postgres/RabbitMQ trước khi API start và healthcheck API trước khi frontend start;
production cần readiness kiểm tra kết nối thật và đưa log/trace vào hệ thống monitoring.

Nếu bật JWT, phải cung cấp Authority và Audience tương ứng với provider đã được duyệt. Không
đưa dev headers vào staging/production; local compose mặc định để JWT disabled.

Subjective grading worker callback cần secret riêng ngoài JWT:

```text
Workers__Callback__SharedSecret=<secret manager value>
Workers__Callback__MaxClockSkewSeconds=300
```

Worker ký raw JSON body bằng HMAC-SHA256 theo timestamp; không truyền secret qua frontend và
không bật callback production nếu secret chưa được cấp.
