# Learning Frontend Module

## Screens

### Trang chủ học tập
- HSK/context hiện tại;
- tiếp tục học;
- nội dung cần ôn ngắn;
- tiến độ tóm tắt.

Không biến thành analytics dashboard lớn.

### Lộ trình HSK
- HSK 1–9;
- current/target level;
- progress;
- topic/unit/lesson tree.

### Người mới bắt đầu
Flow trực quan:

```text
Pinyin
→ Thanh điệu
→ Âm đầu/Vần
→ Ghép âm
→ Chữ Hán/nét cơ bản
→ Bài làm quen
```

### Bài học
- lesson list;
- topic grouping;
- status;
- lesson detail.

## Routes gợi ý

```text
/app
/app/hsk
/app/beginner
/app/lessons
/app/lessons/:id
```

## Components

- LearningHome;
- HskLevelCard;
- CurriculumTree;
- BeginnerStageCard;
- LessonCard;
- LessonOutline;
- ContinueLearningCard.

## API dependencies

Identity profile/context + Curriculum + Learning.

## Computer Use tests

- [ ] Trang chủ → Continue learning;
- [ ] Lộ trình HSK → HSK 3 → lesson;
- [ ] Beginner → Pinyin/tone stage;
- [ ] complete lesson → UI cập nhật;
- [ ] refresh không mất state.
