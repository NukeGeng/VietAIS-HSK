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
- level detail chỉ hiển thị lesson đã publish;
- chọn HSK làm lộ trình hiện tại.

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
- start/complete lesson và giữ trạng thái sau khi reload.

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
- HskLevelDetail;
- BeginnerStageCard;
- LessonCard;
- LessonOutline;
- LessonDetail;
- ContinueLearningCard.

## API dependencies

Identity profile/context + Curriculum + Learning.

## Computer Use tests

- [x] Trang chủ → Continue learning;
- [x] Lộ trình HSK → HSK 3 → lesson;
- [x] Beginner → Pinyin/tone stage;
- [x] Bài học → tìm kiếm/lọc cấp độ → mở lesson detail;
- [x] complete lesson → UI cập nhật;
- [x] refresh không mất state.
