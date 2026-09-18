# Curriculum Model

## HSK path

```text
SyllabusVersion
→ HskLevel
→ Topic
→ Unit
→ Lesson
```

Lesson liên kết:

- learning objectives/tasks;
- Vocabulary;
- Hanzi;
- Grammar;
- skill content;
- practice references.

## Beginner path

```text
BeginnerTrack
→ BeginnerStage
→ BeginnerLesson
```

Stage dự kiến:

```text
Pinyin
→ Thanh điệu
→ Âm đầu / Vần
→ Ghép âm
→ Chữ Hán cơ bản
→ Nét cơ bản
→ Bài làm quen
```

Beginner content có thể tái sử dụng cùng master data Vocabulary/Hanzi/Pinyin thay vì duplicate.

## Foundation entities/documents

```text
PinyinInitial
PinyinFinal
PinyinSyllable
ToneRule
Vocabulary
Hanzi
GrammarPoint
```

## Hanzi relationship

```text
Hanzi
├── pinyin readings
├── meanings
├── radical
├── stroke count
├── stroke data reference
└── related Vocabulary
```

## Versioning

Mọi mapping HSK phải gắn `SyllabusVersion`.

Không trộn HSK legacy và HSK 3.0 trong cùng field không version.
