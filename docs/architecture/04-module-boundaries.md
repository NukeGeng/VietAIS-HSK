# Module Boundaries

## Business modules

### Identity
Sở hữu:
- account/profile;
- learning preferences;
- HSK hiện tại/mục tiêu;
- timezone;
- authorization context.

### Curriculum
Sở hữu nội dung chuẩn:
- HSK Level / Topic / Unit / Lesson;
- Beginner foundation;
- Pinyin;
- thanh điệu;
- Vocabulary;
- Hanzi + metadata/dữ liệu nét;
- Grammar;
- lesson composition.

### Learning
Sở hữu:
- learner enrollment/context;
- selected HSK;
- beginner path progress;
- lesson start/complete;
- learning milestones.

### Practice
Sở hữu:
- practice sessions;
- question attempts;
- deterministic grading;
- Pinyin/tone practice;
- Hanzi writing attempts/validation;
- vocabulary/grammar/listening/reading/writing practice.

### Review
Sở hữu:
- review items;
- review scheduling;
- mistakes;
- nội dung cần ôn.

### Exam
Sở hữu:
- mock exam definition reference;
- ExamAttempt event stream;
- objective grading;
- subjective grading request/result;
- exam result.

### Translation
Sở hữu:
- Việt → Trung attempt;
- reference answer;
- optional AI feedback.

### Speaking
Sở hữu:
- speaking sessions;
- dialogue state;
- speech assessment results;
- AI dialogue/feedback summary.

### Progress
Sở hữu read models/tổng hợp:
- tiến độ;
- mức độ nắm vững;
- điểm yếu;
- lịch sử học;
- streak.

### Content
Sở hữu:
- question bank authoring;
- audio asset jobs/metadata;
- stories;
- videos;
- learning resources;
- approved tools metadata;
- content review/publish hỗ trợ.

## Boundary rule

Module không truy cập internal type/storage của module khác.

Dùng public contract/query/message khi cần.

## Không tạo module theo menu

Không tạo riêng:

- PinyinModule;
- ToneModule;
- HanziWritingModule;
- StreakModule;
- StoryModule;
- VideoModule.

Nếu boundary hiện có đã sở hữu nghiệp vụ đó thì giữ trong module hiện tại.
