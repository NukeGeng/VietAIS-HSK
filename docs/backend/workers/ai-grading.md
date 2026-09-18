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
