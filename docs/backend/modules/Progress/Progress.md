# Progress Module

## 1. Mục đích

Tổng hợp dữ liệu học thành các read model dễ hiểu: tiến độ học, mức độ nắm vững, điểm yếu, lịch sử học và chuỗi ngày học.

Progress chủ yếu là projection/read side, không phải write aggregate lớn.

## 2. Input sources

- Learning lesson/milestone events;
- Practice result messages;
- Review result/activity signals;
- Exam result events;
- Translation activity/result signal;
- SpeakingSessionCompleted.

Không đọc trực tiếp internal tables của module khác để tính mỗi request.

## 3. Chức năng

### 3.1 Tiến độ

Theo:
- HSK level;
- lesson;
- vocabulary;
- Hanzi;
- grammar;
- Nghe/Đọc/Viết/Nói khi data đủ.

### 3.2 Điểm yếu

Knowledge/skill weakness phải có source evidence và dẫn tới action:

```text
Ôn ngay
Luyện lại
Xem bài học
```

Không cần AI recommendation ở MVP; dùng rule/threshold.

### 3.3 Lịch sử học

Timeline từ meaningful activity, không log click/page view.

### 3.4 Chuỗi ngày học

Một ngày được tính khi có ít nhất một qualifying activity hoàn thành, ví dụ:
- lesson completed;
- practice session completed;
- review completed;
- exam submitted;
- speaking session completed;
- translation attempt completed.

Page view/login không tính.

Day boundary theo learner timezone từ Identity; fallback theo system configuration nếu profile chưa có timezone.

## 4. Data model / Projections

Async read models:

```text
UserLearningProgress
UserKnowledgeMastery
UserWeakPoint
UserLearningHistory
UserStudyStreak
```

Bootstrap hiện lưu một `ProgressProjectionDocument` per learner trong Marten: input Practice, Review, Exam, Translation và Speaking có event key ổn định, activity history có key idempotent, cùng snapshot/weak points đã materialize. Kết quả Review đúng trừ một evidence weak-point; Review sai cộng evidence theo rule deterministic; câu sai trong Exam tạo weak-point `ExamIncorrect`. Translation chỉ ghi nhận attempt, không tự chấm đúng/sai; Speaking chỉ ghi nhận session hoàn tất và số lượt nói, không tạo score giả khi provider chưa có. `ProgressProjectionBuilder` dựng lại các giá trị đọc từ inputs đã lưu; đây là document projection, không Event Source aggregate của Progress. Khi không cấu hình PostgreSQL, API giữ in-memory fallback.

Có thể có inline/minimal immediate projection nếu UI cần feedback ngay sau action, nhưng tránh duplicate logic.

## 5. Queries

```text
GetMyProgress
GetMyWeakPoints
GetMyLearningHistory
GetMyStudyStreak
GetLearningHomeSummary
```

Progress thường không cần public commands ngoài maintenance/rebuild admin tooling.

## 6. RabbitMQ

Không yêu cầu cho projection trong cùng process nếu Marten Async Daemon/Wolverine local messaging đủ.

Không đưa mọi progress event ra broker.

## 7. AI

Không.

## 8. Invariants

- user isolation;
- projection replay/rebuild cho kết quả nhất quán;
- duplicate practice event/activity không làm tăng projection lần hai;
- no click-based streak;
- weakness rule version/config traceable khi thay đổi đáng kể.

## 9. Test cases

- [x] practice + review result updates deterministic mastery read model;
- [x] repeated Hanzi writing error tạo weakness; hai completion sai tạo `hanzi-writing/WritingWeak` với evidence cộng dồn;
- [x] review success cải thiện state theo rule;
- [x] exam result cập nhật metrics và weak-point theo câu sai;
- [x] timezone streak boundary: `/api/progress/streak` lấy timezone từ Identity profile và builder có test crossing midnight;
- [x] duplicate event idempotency;
- [x] projection rebuild;
- [x] Computer Use: Progress → Điểm yếu → action link đúng; `WritingWeak` mở `/app/hanzi/{id}/write`;
- [x] Computer Use: Chuỗi ngày học hiển thị đúng sample data.

## 10. Giới hạn bootstrap còn lại

- Hiện đã nối Learning lesson completion, Practice result/session completion và Review item result vào Progress.
- Exam, Translation và Speaking đã phát input signal vào Progress; Translation/Speaking metrics đã có trong snapshot.
- Review result phát `ReviewEvaluationSignal` idempotent vào Progress; hiện dùng evidence weak-point deterministic, chưa có mastery model riêng.
- `/api/progress/streak` lấy timezone từ Identity profile; timezone không hợp lệ bị Identity validation từ chối và builder fallback UTC cho dữ liệu legacy.
