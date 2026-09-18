# Hanzi Writing Data

## Mục đích

Cung cấp dữ liệu tham chiếu để:

- xem thứ tự nét;
- animate nét;
- luyện viết theo hướng dẫn;
- kiểm tra attempt của learner;
- xác định chữ cần ôn.

## Ownership

### Curriculum
Lưu/reference:

```text
Hanzi
HanziStrokeSet
StrokeOrderVersion
Source/License metadata
```

### Practice
Lưu:

```text
HanziWritingAttempt
HanziStrokeAttemptResult
```

### Review
Chỉ nhận `KnowledgeRef(HanziId)` + lý do cần ôn, không duplicate stroke data.

## HanziStrokeSet

Tối thiểu cần:

```text
HanziId
StrokeCount
OrderedStrokes[]
Source
SourceVersion
LicenseRef
```

Mỗi stroke cần đủ dữ liệu cho renderer/validator, ví dụ path/median/normalized geometry tùy dataset thực tế.

Không khóa schema vào một library trước khi PoC xác nhận format.

## Writing attempt

```text
AttemptId
UserId
HanziId
Mode
StartedAt
CompletedAt
StrokeResults[]
OverallResult
```

Mode:

```text
Guided
Trace
Recall
```

## Validation baseline

Không dùng AI.

Validator deterministic có thể kiểm tra theo khả năng dataset:

- số nét;
- thứ tự nét;
- vị trí bắt đầu/kết thúc tương đối;
- hướng nét;
- độ lệch hình dạng/tolerance;
- nét bỏ sót/nét thừa.

Exact tolerance/geometry algorithm phải được benchmark trước khi khóa.

## Result

Không cần điểm 0–100 nếu chưa chứng minh hữu ích.

MVP có thể dùng:

```text
Correct
NeedsRetry
Incorrect
```

kèm lỗi theo stroke.

## Review trigger

Ví dụ:

```text
Sai cùng Hanzi nhiều lần
→ Review.Upsert(HanziId, reason=WritingWeak)
```

Không đưa mọi attempt vào review nếu learner đã làm đúng ổn định.
