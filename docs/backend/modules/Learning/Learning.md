# Learning Module

## 1. Mục đích

Quản lý learner journey: chọn/khởi tạo lộ trình, bắt đầu/hoàn thành bài và các milestone quan trọng cho cả HSK path và Beginner path.

## 2. Phạm vi

### Có làm
- learner enrollment/path context;
- selected HSK learning path;
- beginner path start/progress;
- lesson started/completed;
- stage/unit/level milestones;
- continue-learning state.

### Không làm
- content body;
- scoring exercise;
- review scheduling;
- progress analytics dài hạn;
- streak calculation.

## 3. Actor & phân quyền

| Actor | Quyền |
|---|---|
| Guest | không có personal learning state |
| Learner | thao tác path của chính mình |
| Admin | không sửa learning state learner bằng UI thường; support operation nếu được thiết kế riêng |

## 4. Chức năng

### 4.1 Bắt đầu Beginner path

```text
Learner chọn Người mới bắt đầu
→ StartBeginnerTrack
→ lesson đầu tiên khả dụng
→ complete lesson/stage theo thứ tự rule
```

Không bắt learner phải hoàn thành Beginner trước HSK 1 nếu product không quy định bắt buộc.

### 4.2 HSK learning path

```text
Select HSK level
→ load curriculum tree
→ Start/Resume lesson
→ Complete lesson
→ milestone update
```

### 4.3 Continue learning

Query trả lesson/stage hợp lý gần nhất dựa trên learning state, không dùng AI.

## 5. Invariants

- Learner chỉ mutate stream/state của chính mình.
- LessonId phải thuộc published curriculum khi bắt đầu mới.
- Complete lesson phải idempotent.
- Không đánh dấu level complete chỉ vì user đổi PreferredHskLevel.

## 6. Data model / Event Sourcing

Event stream gợi ý:

```text
LearningPath-{UserId}-{TrackKey}
```

Events:

```text
LearningTrackStarted
HskLevelSelected
LessonStarted
LessonCompleted
BeginnerStageCompleted
UnitCompleted
HskLevelCompleted
```

Không phát event page-view/click.

## 7. Projections

### Inline

`CurrentLearningState`:
- current track;
- current lesson;
- completed lesson ids/count;
- current stage/unit;
- continue target.

### Async

- learning history feed input;
- long-term aggregation input cho Progress.

## 8. Commands

| Command | Mục đích |
|---|---|
| StartBeginnerTrack | bắt đầu beginner |
| SelectHskLevel | chọn context HSK |
| StartLesson | bắt đầu lesson |
| CompleteLesson | hoàn thành lesson |

Milestone nội bộ có thể được derive trong handler/projection, không cần public command cho mọi event.

## 9. Queries

| Query | Mục đích |
|---|---|
| GetMyLearningHome | current HSK/path/next lesson |
| GetMyHskPath | tree + completion |
| GetMyBeginnerPath | beginner stages + completion |
| GetContinueLearningTarget | bài tiếp theo |

## 10. API gợi ý

```text
GET  /api/learning/home
GET  /api/learning/hsk/{level}
POST /api/learning/hsk/{level}/select
GET  /api/learning/beginner
POST /api/learning/beginner/start
POST /api/learning/lessons/{id}/start
POST /api/learning/lessons/{id}/complete
```

## 11. RabbitMQ

Không cho core flow.

Internal Wolverine message/event có thể dùng để Progress/Review cập nhật nếu cần durability, nhưng không bắt buộc đi RabbitMQ.

## 12. AI

Không.

## 13. Dependencies

- Identity: UserId/context;
- Curriculum: published lesson/path reference.

## 14. Test cases

### Unit
- [ ] completion idempotent;
- [ ] wrong user cannot mutate stream;
- [ ] milestone derivation;
- [ ] beginner/HSK path independent.

### Integration
- [ ] event append + inline state;
- [ ] refresh/resume đúng current lesson;
- [ ] curriculum reference validation.

### Computer Use
- [ ] Người mới bắt đầu → bài đầu → complete → next;
- [ ] Lộ trình HSK → chọn HSK 3 → mở bài → complete → reload;
- [ ] continue-learning card đúng.

## 15. Acceptance Criteria

- [ ] learning state reconstruct/replay được;
- [ ] không lưu content duplicate;
- [ ] beginner + HSK path đều hoạt động;
- [ ] no AI/RabbitMQ abuse.
