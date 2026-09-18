# Progress Module

## 1. Mục đích

Tổng hợp dữ liệu học thành các read model dễ hiểu: tiến độ học, mức độ nắm vững, điểm yếu, lịch sử học và chuỗi ngày học.

Progress chủ yếu là projection/read side, không phải write aggregate lớn.

## 2. Input sources

- Learning lesson/milestone events;
- Practice result messages;
- Review result messages;
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
- no click-based streak;
- weakness rule version/config traceable khi thay đổi đáng kể.

## 9. Test cases

- [ ] practice result updates mastery;
- [ ] repeated Hanzi writing error tạo weakness;
- [ ] review success cải thiện state theo rule;
- [ ] timezone streak boundary;
- [ ] duplicate event idempotency;
- [ ] projection rebuild;
- [ ] Computer Use: Progress → Điểm yếu → action link đúng;
- [ ] Computer Use: Chuỗi ngày học hiển thị đúng sample data.
