# Curriculum Module

## 1. Mục đích

Là source of truth cho nội dung học có cấu trúc: HSK 3.0, người mới bắt đầu, Pinyin, thanh điệu, từ vựng, chữ Hán, ngữ pháp và lesson composition.

## 2. Phạm vi

### Có làm
- SyllabusVersion;
- HSK 1–9;
- Topic / Unit / Lesson;
- BeginnerTrack / BeginnerStage / BeginnerLesson;
- Pinyin initials/finals/syllables;
- tone rules;
- Vocabulary;
- Hanzi + stroke metadata/reference;
- GrammarPoint;
- draft/published status;
- mapping knowledge ↔ lesson/topic/level.

### Không làm
- learner progress;
- practice attempt;
- Hanzi writing attempt;
- review schedule;
- audio generation worker;
- story/video extended content body.

## 3. Actor & phân quyền

| Actor | Quyền |
|---|---|
| Guest | đọc public/published curriculum nếu product cho phép preview |
| Learner | đọc published curriculum |
| Admin | CRUD/publish khi có `curriculum.manage` |

Learner không thấy draft content.

## 4. Chức năng

### 4.1 HSK path

```text
SyllabusVersion
→ HskLevel
→ Topic
→ Unit
→ Lesson
```

Lesson reference Vocabulary/Hanzi/Grammar/skills, không copy master data vào lesson document.

### 4.2 Beginner path

```text
Pinyin
→ Thanh điệu
→ Âm đầu/Vần
→ Ghép âm
→ Hanzi/nét cơ bản
→ Bài làm quen
```

Beginner lesson tái sử dụng master entities.

### 4.3 Pinyin & thanh điệu

Master data hỗ trợ:
- học;
- tra cứu;
- Practice tạo bài;
- audio reference nếu có.

### 4.4 Vocabulary

Tối thiểu:
- Simplified;
- Pinyin;
- POS nếu có;
- HSK mapping/version;
- platform Vietnamese meanings/examples qua content enrichment model đã duyệt;
- audio asset reference.

### 4.5 Hanzi

Tối thiểu:
- character;
- reading(s);
- meaning;
- radical;
- stroke count;
- HSK mapping/version;
- `HanziStrokeSetId`/stroke reference;
- related Vocabulary queryable.

Curriculum chỉ sở hữu reference data; Practice sở hữu learner writing attempt.

Bootstrap hiện cung cấp read catalog tối thiểu qua `BootstrapHanziCatalog` với provenance
`PlatformAuthoredReferenceFixture`/`hanzi-reference-v1`/`platform-authored`. Đây là fixture để
kiểm tra contract và UI, không được coi là dữ liệu HSK 3.0 chính thức. Catalog thật phải được
import cùng source/version/license đã duyệt trước khi publish cho learner.

Bootstrap hiện cũng cung cấp read catalog tối thiểu cho Vocabulary và Grammar qua
`BootstrapKnowledgeCatalog`. Các endpoint hỗ trợ `search`, `hsk` và `topic` cho list; detail
tra theo id hoặc khóa nội dung. Mọi record đều ghi provenance
`PlatformAuthoredReferenceFixture`/`knowledge-reference-v1`/`platform-authored`. Đây là dữ liệu
tham chiếu để kiểm tra contract và UI, chưa phải bộ HSK 3.0 chính thức.

### 4.6 Grammar

- HSK mapping/version;
- pattern;
- simple Vietnamese explanation;
- examples;
- common mistakes khi authored;
- lesson/topic references.

### 4.7 Publish

Status tối thiểu:

```text
Draft
Published
Archived
```

Published content không được silently mutate theo cách làm hỏng learner history; thay đổi lớn cần version hoặc content revision strategy.

Import HSK nhận cấu trúc `Topic → Unit → Lesson`; mọi lesson mới ở trạng thái Draft. Admin publish level trước, sau đó publish lesson riêng. Public tree chỉ trả Topic/Unit có ít nhất một Published lesson và learner chỉ có thể bắt đầu lesson đó. Version đã Published chỉ nhận lại import giống hệt như một thao tác idempotent; thay đổi nội dung phải dùng `SyllabusVersion` mới.

## 5. Invariants

- HSK mapping luôn gắn SyllabusVersion.
- HSK level `Id` phải duy nhất trên toàn bộ store; khi tạo syllabus version mới phải dùng level Id
  mới để route/publish không mơ hồ và không làm thay đổi version đã publish.
- Lesson không reference entity không tồn tại.
- Learner chỉ đọc Published.
- Hanzi stroke metadata phải có source/version/license reference khi đến từ external dataset.
- Không duplicate master Vocabulary/Hanzi trong beginner path.

## 6. Data model

### Documents

```text
SyllabusVersion
HskLevel
Topic
Unit
Lesson
BeginnerTrack
BeginnerStage
BeginnerLesson
PinyinInitial
PinyinFinal
PinyinSyllable
ToneRule
Vocabulary
Hanzi
HanziStrokeSet
GrammarPoint
```

### Event streams

Không dùng Event Sourcing cho curriculum CRUD.

### Read models

Có thể dùng Marten document/query trực tiếp; chỉ projection khi cần denormalized catalog hiệu năng.

## 7. Commands

| Command | Mục đích |
|---|---|
| ImportHskDataset | import official/reference dataset đã validate |
| UpsertVocabulary | quản trị vocabulary |
| UpsertHanzi | quản trị Hanzi |
| UpsertGrammarPoint | quản trị grammar |
| UpsertPinyinData | quản trị Pinyin |
| SaveLessonDraft | lưu lesson draft |
| PublishLesson | publish lesson |
| PublishCurriculumNode | publish node được phép |

## 8. Queries

| Query | Mục đích |
|---|---|
| GetHskLevels | level list |
| GetCurriculumTree | topic/unit/lesson tree |
| GetBeginnerTrack | beginner path |
| SearchVocabulary | tra cứu từ |
| SearchHanzi | tra cứu chữ |
| GetHanziStrokeData | renderer/Practice cần stroke reference |
| SearchGrammar | tra cứu grammar |
| GetPinyinFoundation | Pinyin/tone learning data |

## 9. Events/messages

Không Event Source.

Có thể phát `CurriculumContentPublished` khi module khác cần invalidate cache/index; không bắt buộc cho MVP.

## 10. API gợi ý

Public/learner:

```text
GET /api/curriculum/hsk-levels
GET /api/curriculum/hsk/{level}/tree
GET /api/curriculum/lessons/{id}
GET /api/curriculum/beginner
GET /api/vocabulary
GET /api/vocabulary/{id}
GET /api/hanzi
GET /api/hanzi/{id}
GET /api/hanzi/{id}/strokes
GET /api/grammar
GET /api/grammar/{id}
GET /api/foundation/pinyin
GET /api/foundation/tones
```

`GET /api/vocabulary` và `GET /api/grammar` nhận query tùy chọn `search`, `hsk`, `topic`.

Admin endpoints nằm dưới `/api/admin/...` và yêu cầu `curriculum.manage`.

Import dùng lại level Id đã thuộc version khác trả `409 Conflict`; retry cùng version và payload
đã import là idempotent.

```text
GET  /api/admin/curriculum/hsk-levels
POST /api/admin/curriculum/hsk-levels/{levelId}/lessons/{lessonId}/publish
```

## 11. RabbitMQ

Không cho CRUD curriculum.

Audio generation được gửi qua Content/Media workflow, không chạy model trong Curriculum handler.

## 12. AI

Không.

## 13. Dependencies

Không phụ thuộc learner modules.

Content có thể cung cấp AudioAsset metadata; tránh circular dependency bằng ID/contract đơn giản.

## 14. Test cases

### Unit
- [x] syllabus version mapping;
- [x] publish validation;
- [x] lesson reference validation;
- [x] Hanzi stroke metadata source required.

### Integration
- [x] import idempotency/duplicate handling;
- [x] published-only learner queries;
- [x] published lesson read contract returns only Published lessons;
- [x] beginner path references master data;
- [x] Hanzi stroke read endpoint (bootstrap reference fixture);

### Permission
- [x] learner không mutate curriculum;
- [x] admin thiếu permission không publish.

### Computer Use
- [x] Người mới bắt đầu load Pinyin/tone;
- [x] Nền tảng → Chữ Hán → xem stroke order (fixture);
- [x] search vocabulary/grammar đúng filter HSK.
- [x] Lộ trình HSK → mở lesson Published → start/complete.

## 15. Acceptance Criteria

- [x] HSK 3.0 import contract và Beginner data có model rõ; dataset HSK/CTI chính thức vẫn
  được nạp qua import sau khi provenance được duyệt;
- [x] Pinyin/tone/Hanzi writing reference data đủ cho UI/Practice fixture; catalog chính thức
  vẫn tách khỏi fixture platform-authored;
- [x] no learner progress stored here;
- [x] no AI/RabbitMQ misuse.
