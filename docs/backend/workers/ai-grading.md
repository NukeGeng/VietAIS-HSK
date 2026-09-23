# AI Grading Worker

Nhận job chấm phần tự luận của bài test lớn.

Không chấm objective question.

Phải đảm bảo:
- idempotent;
- timeout;
- retry có giới hạn;
- structured response;
- lưu trạng thái thất bại;
- không làm mất ExamAttempt.

## Boundary đã triển khai

`backend/VietAisHsk.Workers` consume `vietais.exam.subjective-grading` bằng Wolverine/RabbitMQ,
ghi durable inbox ở PostgreSQL và gọi `POST /api/exam-attempts/{id}/subjective-grading` bằng HMAC.
Callback giữ `JobId`, `UserId`, `SchemaVersion` và trạng thái Failed/Completed.

Provider mặc định fail-closed (`UnconfiguredSubjectiveGradingProvider`) để không giả lập AI khi
chưa có provider/decision record. Khi provider thật được chọn, chỉ thay adapter trong worker;
không đổi Exam state machine hoặc callback contract.
