# Messaging Building Block

Bao gồm:
- Wolverine configuration;
- inbox/outbox;
- RabbitMQ endpoints;
- retry;
- dead-letter handling;
- message naming;
- correlation ID.

## Current implementation

- `WolverineFx.Marten` và `WolverineFx.RabbitMQ` được tham chiếu trong
  `backend/VietAisHsk.Api`.
- `WolverineFx.RuntimeCompilation` cũng được tham chiếu vì container production bật dynamic
  handler generation; thiếu package này làm API fail ngay khi khởi động dù RabbitMQ reachable.
- Local mặc định vẫn dùng in-memory queue để API, test và Computer Use không phụ thuộc
  broker ngoài.
- Production adapter chỉ bật khi đồng thời có:
  - `Messaging:EnableWolverine=true`;
  - `ConnectionStrings:Postgres`;
  - `ConnectionStrings:RabbitMq` dạng `amqp://...`.
- Khi bật, Marten được tích hợp với Wolverine và hai contract async được route riêng:
  - `AudioGenerationRequested` → `vietais.audio.generate`;
  - `SubjectiveGradingRequestedMessage` → `vietais.exam.subjective-grading`.
- Các endpoint CRUD, Review/Progress signal nội process và Speaking per-turn không đi qua
  RabbitMQ.
- Queue adapter được đăng ký singleton nhưng resolve `IMessageBus` qua `IServiceScopeFactory`
  trong mỗi lần enqueue, vì Wolverine đăng ký bus theo scoped lifetime. Điều này giữ đúng lifetime
  khi gọi từ các store singleton và đã được kiểm chứng bằng compose stack thật.

Worker consumers và dead-letter operational policy vẫn được triển khai ở worker/deploy phase;
callback authentication boundary đã có HMAC contract, local queue không được gọi là production
broker.

Verification 2026-09-23: solution build pass, 98/98 backend tests pass, identity/learning smoke
pass sau khi restart API sạch. Compose QA có PostgreSQL/RabbitMQ healthy, API `/health` trả 200,
frontend container trả 200 và hai smoke suite pass qua API container. Không giả lập RabbitMQ
runtime khi broker chưa được cấu hình.
