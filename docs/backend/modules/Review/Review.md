# Review Module

## 1. Mục đích

Quản lý nội dung learner cần ôn lại, lịch ôn và câu làm sai.

UI gọi là `Ôn tập`, không cần hiển thị thuật ngữ SRS.

## 2. Phạm vi

- review queue;
- review scheduling;
- vocabulary/Hanzi/grammar review;
- Hanzi writing weak review;
- mistakes;
- `Nội dung cần ôn` tổng hợp theo reason/priority;
- review result signal cho Progress.

Không sở hữu master knowledge data.

## 3. Actor & phân quyền

| Actor | Quyền |
|---|---|
| Guest | không personal review state |
| Learner | review/mistakes của chính mình |
| Admin | không sửa review queue learner theo UI thường |

## 4. Chức năng

### 4.1 Upsert ReviewItem

Input từ Practice/Learning/Exam có thể tạo/cập nhật item:

```text
UserId
KnowledgeType
KnowledgeId
Reason
Priority/Strength metadata
```

Reason ví dụ:

```text
WrongAnswer
RepeatedMistake
WritingWeak
LowMastery
ScheduledReview
```

### 4.2 Hôm nay cần ôn

Query lấy item đến hạn theo scheduler deterministic.

Exact scheduling algorithm là open decision; không dùng AI.

### 4.3 Câu làm sai

Lưu/reference:
- question;
- answer user;
- correct answer/result;
- mistake count;
- last attempted;
- resolved status nếu có.

### 4.4 Hanzi writing review

Sai viết nhiều lần có thể tạo `ReviewItem(HanziId, WritingWeak)`.

Khi review, Practice Hanzi validator vẫn là nơi chấm; Review chỉ orchestration/schedule.

## 5. Invariants

- One active ReviewItem per `(UserId, KnowledgeType, KnowledgeId, Reason)` hoặc uniqueness rule tương đương.
- Learner chỉ xem/mutate item của mình.
- Review completion idempotent.
- Không duplicate full knowledge content.

## 6. Data model

```text
ReviewItem
ReviewHistoryEntry
MistakeItem
```

Không Event Source ở MVP.

## 7. Commands

| Command | Mục đích |
|---|---|
| UpsertReviewItem | internal consumer tạo/cập nhật item |
| StartReviewSession | tạo session review |
| RecordReviewResult | cập nhật lịch/strength |
| MarkMistakeUnderstood | giảm/resolve mistake theo rule |

## 8. Queries

| Query | Mục đích |
|---|---|
| GetTodayReviewSummary | số item theo loại |
| GetDueReviewItems | queue hiện tại |
| GetMistakes | câu sai |
| GetItemsToReview | màn Nội dung cần ôn |

## 9. API gợi ý

```text
GET  /api/review/summary
GET  /api/review/items
POST /api/review/sessions
POST /api/review/sessions/{id}/results
GET  /api/review/mistakes
GET  /api/review/needs-review
```

## 10. RabbitMQ

Không cần broker cho core review.

Consume internal result messages qua Wolverine khi phù hợp.

## 11. AI

Không.

## 12. Dependencies

- Curriculum để render knowledge;
- Practice/Exam/Learning result messages làm input;
- Progress consume review result.

## 13. Test cases

### Unit
- [ ] duplicate upsert không tạo duplicate item;
- [ ] due calculation;
- [ ] writing weak reason;
- [ ] mistake count/update;
- [ ] user isolation.

### Integration
- [ ] Practice wrong → review item;
- [ ] Hanzi repeated wrong → WritingWeak;
- [ ] review complete → next due updated;
- [ ] mistake list persistence.

### Computer Use
- [ ] Ôn tập summary;
- [ ] Nội dung cần ôn filter;
- [ ] câu sai → làm lại;
- [ ] Hanzi writing item → mở đúng luyện viết.

## 14. Acceptance Criteria

- [ ] UI có Ôn tập/Câu làm sai/Nội dung cần ôn;
- [ ] scheduler deterministic;
- [ ] không dùng AI;
- [ ] Hanzi writing weakness tích hợp đúng.
