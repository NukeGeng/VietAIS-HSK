# Practice Frontend Module

## Screens

### Luyện tập tổng
- Từ vựng;
- Chữ Hán;
- Ngữ pháp;
- Nghe;
- Đọc;
- Viết.

### Pinyin / Thanh điệu practice
Có thể được mở từ Người mới/Nền tảng nhưng dùng same practice session components.

### Focus session

```text
Context/tiến độ
→ câu/nhiệm vụ
→ input
→ Kiểm tra
→ feedback
→ Tiếp tục
```

Question renderer hỗ trợ hai dạng: `options` dùng button lựa chọn cho câu multiple-choice;
câu không có options dùng text input. Cả hai gửi cùng Practice answer contract và không nhận
accepted answer từ API.

## Hanzi writing UI

Là first-class screen/component.

Cần:
- character/reference;
- stroke animation/help;
- canvas;
- mode Guided/Trace/Recall;
- per-stroke feedback;
- retry;
- complete result.

Không hiển thị AI branding.

## Components

- PracticeShell;
- QuestionRenderer;
- AnswerFeedback;
- AudioQuestion;
- SentenceArrange;
- PinyinExercise;
- ToneExercise;
- HanziWritingCanvas;
- StrokeGuide;
- StrokeFeedback.

## Computer Use tests

- [x] vocabulary question;
- [x] Pinyin practice: `/app/pinyin` mở `/app/practice/pinyin`, trả lời Pinyin của `你好` không dấu thanh và hoàn tất `1/1` bằng platform-authored fixture;
- [x] tone practice;
- [x] Hanzi wrong stroke → retry;
- [x] Hanzi complete;
- [x] mobile canvas usable;
- [x] incorrect item dẫn tới review state sau integration.
- [x] Practice session refresh/resume: trả lời một câu, refresh, giữ lại câu trả lời và trạng thái `Correct`, sau đó complete.
