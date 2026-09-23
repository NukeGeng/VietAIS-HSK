# Fixed

- `LEARNING-FE-FIX-001` — HomeView có trạng thái loading/error rõ ràng, không render dashboard giả khi API chưa trả dữ liệu.
- `LEARNING-FE-FIX-002` — Dashboard gọi `/api/learning/home` và `/api/progress` song song; nút tiếp tục có fallback về lộ trình HSK hoặc beginner.
- `LEARNING-FE-FIX-003` — Trang Lộ trình HSK và Người mới bắt đầu đã gọi API thật, hiển thị loading/error/empty, start beginner và danh sách 7 stage nền tảng.
- Computer Use: `/app/hsk`, `/app/beginner`, start beginner — pass.
- `LEARNING-FE-FIX-004` — Thay placeholder của `/app/pinyin` và `/app/tones` bằng FoundationView gọi public foundation API; có trạng thái tải/lỗi, danh mục tra cứu, ví dụ, provenance và layout co giãn theo viewport.
- Beginner chỉ liên kết trực tiếp tới hai stage đã có nội dung; các bước chưa triển khai được hiển thị “Đang chuẩn bị”.
- Automated: `npm run build` — pass; `dotnet test VietAisHsk.slnx --no-restore -m:1` — pass, 11/11.
- Computer Use: Beginner → Pinyin, tra cứu `zh`, chuyển Thanh điệu — pass ở mobile 390×844 và desktop 1440×900; document width khớp viewport, không overflow ngang.
- `LEARNING-FE-FIX-005` — Thêm route `/app/hsk/:level` đọc cây HSK Published và `/app/lessons/:id` đọc lesson detail; nối chọn lộ trình, start/complete lesson với Learning state.
- Sửa type adapter `/api/learning/home` để giữ đầy đủ `startedLessonIds`, `completedLessonIds`, `updatedAt`.
- Thêm spacing/responsive cho cây Topic → Unit → Lesson để topic/unit không dính chữ trên desktop và mobile.
- Automated: `npm run build` — pass; `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 38/38.
- Computer Use: HSK 3 detail → mở lesson Published → bắt đầu → hoàn thành → reload giữ “Đã hoàn thành” — pass trên local Vite/API fixture.
- `LEARNING-FE-FIX-006` — Thay `/app/lessons` PageView tĩnh bằng `LessonsView.vue`: đọc toàn bộ HSK level/tree đã publish, flatten Topic → Unit → Lesson, có tìm kiếm/lọc cấp độ, loading/error/empty state và link detail.
- Automated: `npm run build` — pass.
- Computer Use: với curriculum fixture đã publish, `/app/lessons` hiển thị 2 lesson thật, tìm kiếm `Hỏi lịch` lọc còn 1 lesson và mở đúng `/app/lessons/:id` — pass.
- `LEARNING-FE-FIX-007` — HomeView không còn dùng lesson id/progress hardcode: tiến độ được tính từ lesson đã publish và completed state, HSK/lesson tiếp theo đọc từ Curriculum/Learning API; khi chưa có dữ liệu hiển thị empty state.
- `LEARNING-FE-FIX-008` — LearningPathView reload theo cặp `mode/level`, reset state trước mỗi request để HSK index → level detail → beginner không giữ dữ liệu route cũ.
- Computer Use QA 2026-09-22: `/app/hsk` → `/app/hsk/{level}` → `/app/beginner`; mỗi route render đúng nội dung sau khi chuyển cùng component — pass.
- `LEARNING-FE-FIX-009` — Computer Use retest HSK lesson flow với curriculum có topic/unit/lesson đã publish: mở level → mở lesson → bắt đầu → hoàn thành → reload vẫn hiển thị `Đã hoàn thành`.
- Computer Use QA 2026-09-22: Vite `5174`, user `computer-use-user` — pass.
- `LEARNING-FE-FIX-010` — FoundationView đọc learner state và cho phép bắt đầu/hoàn tất Pinyin hoặc Thanh điệu qua Learning API; hiển thị đúng trạng thái và thông báo sau action.
- Computer Use QA 2026-09-22: `/app/pinyin` → `Bắt đầu bước này` → `Hoàn tất bước này` → `/app/tones`; không clipping nội dung tiếng Việt/chữ Hán — pass.
- `LEARNING-FE-FIX-011` — HomeView ưu tiên lesson đang dở từ `LearningHome.continueTarget`, thêm action `Tiếp tục học →` cạnh ngữ cảnh HSK và route đúng cả HSK lesson lẫn Beginner stage.
- Computer Use QA 2026-09-22: `/app` hiển thị `Tiếp tục học →` cho lesson `qa-home-lesson`; bấm action chuyển đúng `/app/lessons/qa-home-lesson` — pass.
- `LEARNING-FE-FIX-013` — Computer Use responsive QA cho HomeView ở 390×844: context HSK, progress, kỹ năng và empty next-lesson đều nằm trong viewport, không clipping/overflow.
- `LEARNING-FE-FIX-012` — LearningPathView hiển thị trạng thái lesson `Chưa học/Đang học/Đã hoàn tất`, trạng thái unit và tổng kết cấp độ từ `completedUnitIds/isLevelCompleted`.
- Computer Use QA: fixture HSK 3 có 2 lesson → mở/bắt đầu/hoàn thành cả hai → quay lại `/app/hsk/ui-hsk3`; UI hiển thị unit đã hoàn tất, cả hai lesson `Xem lại` và cấp độ `Đã hoàn tất các bài hiện có` — pass.
- `LEARNING-FE-FIX-014` — Computer Use responsive regression cho Beginner/Foundation và filter knowledge: Beginner render đủ 7 stage ở 390×844; mở Pinyin và Thanh điệu; Vocabulary chọn HSK 3 còn 2 record; Grammar chọn HSK 3 giữ 4 cấu trúc, không clipping/overflow.
- `LEARNING-FE-FIX-015` — HomeView dùng `currentLevelLabel` cho badge section Kỹ năng thay vì hardcode `HSK 3`, giữ nhất quán với level đang chọn trong sidebar và progress card.
- Computer Use QA 2026-09-23: production Vite `/app` với learner đang chọn HSK 1 hiển thị đồng nhất `HSK 1` ở sidebar, progress, next lesson và section Kỹ năng; không còn nhãn lệch — pass.
- `LEARNING-FE-FIX-016` — `LessonDetailView` reload theo `props.id`, reset dữ liệu cũ trong lúc tải và bỏ qua response của route trước khi user chuyển nhanh giữa các lesson detail.
- Automated: `npm run build` — pass sau khi thêm watcher/race guard.
- `LEARNING-FE-FIX-017` — Curriculum lesson detail trả kèm level/topic/unit context; `LessonDetailView` dùng `lesson.level.displayName` thay vì nhãn HSK hardcode.
- Automated: API test `CurriculumStoreTests.Published_tree_contains_only_published_lessons_and_learning_guard_matches_it` kiểm tra context HSK 3/topic/unit; `npm run build` — pass.
- Computer Use QA 2026-09-23: live Vite/API route `/app/lessons/completion-f834473d-14b2-428f-9a90-4c0ee9f591ed-lesson` hiển thị `HSK 1`, không còn `HSK 3` hardcode — pass.

## Giới hạn còn lại
