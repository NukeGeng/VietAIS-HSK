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

- [ ] vocabulary question;
- [ ] Pinyin/tone;
- [ ] Hanzi wrong stroke → retry;
- [ ] Hanzi complete;
- [ ] mobile canvas usable;
- [ ] incorrect item dẫn tới review state sau integration.
