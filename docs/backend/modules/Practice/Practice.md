# Practice Module

## 1. Mục đích

Cung cấp các buổi luyện tập và chấm deterministic cho kiến thức/kỹ năng mà hệ thống có thể đánh giá bằng rule/reference data.

## 2. Phạm vi

### Có làm
- Pinyin;
- thanh điệu;
- từ vựng;
- chữ Hán;
- luyện viết chữ Hán;
- ngữ pháp;
- nghe;
- đọc;
- viết dạng có đáp án/rule rõ;
- question attempt/results;
- tạo signal cho Review/Progress.

### Không làm
- Dịch Việt - Trung open-ended AI;
- Speaking dialogue;
- mock exam orchestration;
- curriculum authoring.

## 3. Actor & phân quyền

| Actor | Quyền |
|---|---|
| Guest | demo practice nếu product cho phép; không lưu personal state |
| Learner | tạo/làm practice của chính mình |
| Admin | quản lý question/content qua Content, không sửa attempt learner |

## 4. Chức năng

### 4.1 Practice session

```text
Chọn loại/HSK/topic
→ tạo session từ eligible questions
→ answer
→ deterministic grading
→ feedback
→ complete
→ publish learning result signal
```

### 4.2 Pinyin & thanh điệu

Dạng:
- nhận diện âm;
- ghép initial/final;
- chọn tone;
- nghe phân biệt;
- mapping Pinyin ↔ Hanzi khi phù hợp.

### 4.3 Vocabulary / Grammar / Nghe / Đọc

Chấm bằng answer key/rule.

### 4.4 Viết deterministic

Ví dụ:
- sắp xếp từ;
- điền từ có accepted answers;
- câu theo pattern đã định nghĩa.

Nếu cần hiểu ngôn ngữ tự nhiên mở thì chuyển đúng use case sang Translation/Exam, không tự gọi AI trong Practice.

### 4.5 Luyện viết chữ Hán

Flow:

```text
Load Hanzi + HanziStrokeSet
→ chọn mode Guided/Trace/Recall
→ user vẽ stroke
→ normalize input
→ validate từng stroke
→ feedback ngay
→ complete attempt
→ nếu yếu, gửi signal cho Review/Progress
```

Validation không dùng AI.

Kiểm tra theo khả năng dataset/PoC:
- stroke count;
- order;
- direction;
- relative start/end;
- geometric tolerance;
- missing/extra stroke.

Không yêu cầu điểm 0–100 ở MVP.

Result baseline:

```text
Correct
NeedsRetry
Incorrect
```

## 5. Invariants

- Answer grading deterministic/reproducible.
- User chỉ mutate attempt của mình.
- Question/content reference phải tồn tại/published khi session bắt đầu.
- Hanzi attempt phải dùng stroke dataset version cố định trong attempt để tránh result thay đổi giữa session.
- Complete session idempotent.
- PracticeSession phải tiếp tục được đọc/hoàn thành sau refresh hoặc API restart khi dùng PostgreSQL.

## 6. Data model

### Documents

```text
PracticeSession
QuestionAttempt
PracticeResult
HanziWritingAttempt
```

Question definition thuộc Content/QuestionBank; knowledge reference thuộc Curriculum. Session state được lưu như Marten document khi có PostgreSQL; không event-source toàn bộ session. `AcceptedAnswers` chỉ tồn tại trong private store model, không có trong learner view.

### Events/messages

Có thể phát internal durable messages:

```text
PracticeCompleted
KnowledgeEvaluated
HanziWritingEvaluated
```

cho Review/Progress.

Không cần Event Source toàn bộ PracticeSession.

## 7. Commands

| Command | Mục đích |
|---|---|
| StartPracticeSession | tạo session |
| SubmitPracticeAnswer | chấm câu |
| CompletePracticeSession | kết thúc |
| StartHanziWritingAttempt | bắt đầu viết chữ |
| SubmitHanziStroke | validate stroke |
| CompleteHanziWritingAttempt | kết thúc attempt |

## 8. Queries

| Query | Mục đích |
|---|---|
| GetPracticeOptions | loại bài/filter khả dụng |
| GetPracticeSession | state hiện tại |
| GetPracticeResult | kết quả |
| GetHanziWritingReference | stroke data + mode data |

## 9. API gợi ý

Bootstrap API hiện có thêm `GET /api/practice/questions` để learner nhận question metadata đã publish; answer key không nằm trong public response.

```text
POST /api/practice/sessions
GET  /api/practice/sessions/{id}
POST /api/practice/sessions/{id}/answers
POST /api/practice/sessions/{id}/complete
POST /api/practice/hanzi/{hanziId}/attempts
POST /api/practice/hanzi/attempts/{id}/strokes
POST /api/practice/hanzi/attempts/{id}/complete
```

## 10. RabbitMQ

Không cho core grading.

Practice result → Review/Progress hiện dùng internal signal contracts và Marten-backed sinks trong bootstrap; durable Wolverine/local messaging có thể thay thế khi cần, không cần RabbitMQ broker.

## 11. AI

Không.

## 12. Dependencies

- Curriculum: knowledge/stroke reference;
- Content: question bank/audio assets;
- Review/Progress là consumer của result signal, tránh Practice gọi trực tiếp storage của họ.

## 13. Test cases

### Unit
- [x] multiple-choice grading (Content options không lộ accepted answers);
- [x] accepted-answer grading;
- [x] tone/Pinyin grading;
- [x] Hanzi stroke order/direction/tolerance fixtures;
- [x] completion idempotency.

### Integration
- [x] session persistence;
- [x] published question selection;
- [x] result signal delivered;
- [x] stroke dataset version captured.

### Computer Use
- [x] Pinyin practice;
- [x] tone practice;
- [x] vocabulary practice;
- [x] Hanzi Guided → sai nét → feedback → retry;
- [x] Hanzi Recall complete → result;
- [x] incorrect item xuất hiện trong Nội dung cần ôn theo rule.

## 14. Acceptance Criteria

- [x] deterministic grading không gọi AI;
- [x] Hanzi writing là first-class practice;
- [x] lỗi/signal đủ cho Review/Progress;
- [x] refresh không mất active session ngoài rule được chấp nhận.
