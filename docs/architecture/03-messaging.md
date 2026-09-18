# Messaging

Wolverine quản lý message handlers và durable messaging.

RabbitMQ chỉ dùng cho:
- AI chấm bài lớn;
- CosyVoice audio;
- background analysis;
- job nền cross-process.

CRUD bình thường:
HTTP + Marten.

Không route tất cả command qua RabbitMQ.
