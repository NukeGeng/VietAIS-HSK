# Content Module

## 1. Mục đích

Quản trị nội dung bổ trợ và assets dùng bởi hệ thống: question bank, exam content reference, audio, truyện song ngữ, video học, tài liệu, công cụ và publish workflow tương ứng.

Curriculum vẫn sở hữu cấu trúc HSK/lesson/master knowledge.

## 2. Phạm vi

### Core content
- QuestionBank;
- question versions/tags;
- exam assembly content/reference;
- AudioAsset + generation jobs;
- publish/review metadata.

### Extended content
- Story;
- VideoLesson/VideoResource;
- LearningResource;
- ToolDefinition.

Không tạo CMS generic ngoài nhu cầu trên.

## 3. Actor & phân quyền

| Actor | Quyền |
|---|---|
| Guest | đọc public published content nếu cho phép |
| Learner | đọc published content |
| Admin | manage với `content.manage` |

## 4. Question bank

Question metadata:
- type;
- HSK/skill/knowledge tags;
- prompt/content;
- answer key/rubric;
- explanation;
- difficulty metadata;
- status/version.

Practice/Exam sử dụng published/versioned question reference.

## 5. Audio

Flow:

```text
Admin/import yêu cầu audio
→ AudioGenerationRequested
→ Wolverine Outbox
→ RabbitMQ audio.generate
→ CosyVoice worker
→ file storage
→ AudioGenerated
→ AudioAsset Ready
```

Trạng thái:

```text
Pending
Processing
Ready
Failed
```

Không gọi CosyVoice khi learner bấm play.

## 6. Stories

Tối thiểu:
- Chinese content;
- Vietnamese translation;
- optional Pinyin;
- optional audio;
- HSK/difficulty tags;
- publish status.

## 7. Video học

- media/source reference;
- transcript;
- optional Pinyin/translation;
- HSK/difficulty tags;
- publish status.

## 8. Tài liệu

- file/link metadata;
- level/topic tags;
- publish status.

## 9. Công cụ

`ToolDefinition` chỉ cho utility đã duyệt và route/config đơn giản.

Không xây plugin marketplace/tool execution platform ở scope này.

## 10. Data model

```text
Question
QuestionVersion (nếu cần version tách)
AudioAsset
AudioGenerationJob
Story
VideoContent
LearningResource
ToolDefinition
```

Không Event Source CRUD content.

## 11. Commands

- SaveQuestionDraft;
- PublishQuestion;
- RequestAudioGeneration;
- RetryAudioGeneration;
- Save/PublishStory;
- Save/PublishVideo;
- Save/PublishResource;
- Save/PublishToolDefinition.

## 12. Queries

- SearchPublishedQuestions (internal contract);
- GetAudioAsset;
- GetStories/GetStory;
- GetVideos/GetVideo;
- GetResources;
- GetTools.

## 13. RabbitMQ

Có cho audio generation.

Không cần cho Story/Video/Resource CRUD.

## 14. AI

Không dùng DeepSeek cho runtime Content module.

AI-assisted authoring offline/admin có thể là future tool nhưng không thuộc scope hiện tại.

## 15. Dependencies

- Storage building block;
- Messaging building block;
- CosyVoice worker;
- Curriculum references/tags bằng contract/IDs.

## 16. Test cases

- [ ] unpublished content không tới learner;
- [ ] question version/reference stable;
- [ ] audio outbox/message idempotent;
- [ ] retry failed audio;
- [ ] story/video/resource filters;
- [ ] permission admin;
- [ ] Computer Use admin audio status;
- [ ] Computer Use learner mở Story/Video/Tài liệu/Công cụ published.
