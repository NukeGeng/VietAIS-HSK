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

Bootstrap hiện có `IQuestionBank`/`BootstrapQuestionBank` cho read contract nội bộ của Practice.
Catalog gồm một số fixture platform-authored cho vocabulary, tone, Hanzi, grammar, listening,
reading và writing; mỗi record giữ status/source/version/license và `ContentVersion`. Learner đọc
qua `GET /api/content/questions` hoặc Practice, còn admin có thể xem draft, lưu draft và publish
qua `content.manage`. Đây vẫn là fixture để nối flow, chưa phải question bank HSK 3.0 chính thức.

Extended content bootstrap đã có read/publish contract riêng: learner chỉ nhận record `Published`,
có filter HSK/topic cho Story/Video/Resource; admin có thể xem draft và publish bằng permission
`content.manage`. Đây là fixture có provenance, chưa phải CMS authoring đầy đủ.

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

### Local contract đã triển khai

Backend hiện có `AudioAsset`, `IAudioAssetStore` và `IAudioGenerationQueue` deterministic cho
local/dev. `POST /api/admin/content/audio` yêu cầu `content.manage`, tạo asset `Pending` và
enqueue đúng một `AudioGenerationRequested` theo `IdempotencyKey`; request trùng trả lại asset
cũ, request trùng key nhưng khác nội dung trả `409`. `POST /api/admin/content/audio/{id}/retry`
chỉ nhận asset `Failed`, tăng `AttemptCount` và tạo idempotency key cho attempt mới.

`GET /api/admin/content/audio` phục vụ status cho admin; learner chỉ đọc được asset `Ready` qua
`GET /api/content/audio/{id}`. Queue hiện là local seam để kiểm thử contract; adapter Wolverine /
RabbitMQ, worker CosyVoice và storage thật vẫn là bước production cần cấu hình sau.

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
QuestionVersion (hoặc `ContentVersion` trên question ở bootstrap)
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

- SearchPublishedQuestions (public read view không có answer key);
- GetAdminQuestions;
- SaveQuestionDraft;
- PublishQuestion;
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

- [x] unpublished content không tới learner;
- [x] question version/reference stable trong practice session;
- [x] local audio request/queue idempotent; [x] optional Wolverine/RabbitMQ adapter được route
  vào `vietais.audio.generate` khi production config bật; [ ] CosyVoice worker/outbox
  operational deployment;
- [x] retry failed audio;
- [x] story/video/resource filters;
- [x] permission admin cho extended-content list/publish;
- [x] permission admin cho question draft/list/publish;
- [x] Computer Use admin audio status;
- [x] Computer Use learner mở Story/Video/Tài liệu/Công cụ published.
