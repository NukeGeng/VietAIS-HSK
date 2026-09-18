# Regression Tests

Sau mỗi fix/module completion phải chạy regression phù hợp dependency graph.

## Critical flow set

- auth/profile;
- beginner path;
- HSK path + lesson completion;
- Pinyin/tone practice;
- vocabulary/Hanzi/grammar foundation;
- Hanzi stroke view + writing;
- practice → review;
- mistakes/needs-review;
- exam start/resume/submit/result;
- translation optional feedback;
- speaking session;
- progress/weak points/history/streak;
- admin content publish/audio job khi đã implement.

Không bắt chạy toàn bộ suite browser cho thay đổi nhỏ nếu dependency không liên quan; nhưng critical regression phải chạy trước release/staging promotion.
