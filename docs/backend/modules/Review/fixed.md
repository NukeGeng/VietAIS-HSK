# Fixed

## REVIEW-FIX-001 — Practice signal to deterministic review queue

### Thay đổi

- Thêm `PracticeEvaluationSignal` contract nội bộ giữa Practice và Review.
- Câu sai tạo/merge một `ReviewItem` theo composite identity, tăng mistake count và đưa item về due ngay.
- Câu đúng giảm priority; review result đúng có thể resolve item và schedule lần kế tiếp.
- Thêm summary, items, needs-review, mistakes, review session và result endpoints.
- Giữ scheduler bootstrap deterministic; algorithm cuối cùng vẫn là open decision.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm Practice wrong → review item → review result → resolve.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Review không sở hữu master knowledge data.
- User isolation được kiểm tra qua context hiện tại.
- Không dùng AI/RabbitMQ cho core review bootstrap.

## REVIEW-FIX-002 — Marten persistence cho review queue và result

### Thay đổi

- Đăng ký `MartenReviewStore` và schema cho `ReviewItem`/`ReviewSession` khi có PostgreSQL; giữ in-memory fallback khi không cấu hình DB.
- Practice wrong signal upsert ReviewItem theo composite identity; item và review session được lưu qua Marten.
- Ghi kết quả lưu nguyên tử item cùng `RecordedResults`; retry cùng kết quả trả lại item cũ, retry mâu thuẫn không ghi đè.
- Thêm kiểm tra duplicate upsert, mistake count, due scheduling và user isolation.

### Kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 24/24.
- `node --check scripts/marten-persistence-smoke.mjs` — pass.
- PostgreSQL restart smoke ba pha: Practice wrong → tạo ReviewItem/ReviewSession → restart/đọc lại → ghi kết quả + retry idempotent → restart/verify resolved — pass.
- Smoke xác nhận learner khác không đọc được item; retry mâu thuẫn nhận 404.

### Ghi chú môi trường

- API smoke trong sandbox cần `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false` vì config file-watcher làm `WebApplication.CreateBuilder` treo trước khi host khởi động. Đây chỉ là biến chạy QA, không đổi cấu hình runtime sản phẩm.

### Giới hạn còn lại

- Progress projection is now persisted by `PROGRESS-FIX-002`; Exam/Translation/Speaking inputs and review-success mastery effects remain pending.
- Hanzi writing weak reason và integration từ Learning/Exam chưa được nối.

## REVIEW-FIX-003 — Review session UI nối API thật

### Thay đổi

- `ReviewView` đọc summary cùng queue đến hạn, hoặc đọc danh sách `mistakes`/`needs-review` theo route.
- Thêm thao tác bắt đầu `ReviewSession` bằng đúng các item đang hiển thị.
- Thêm thao tác ghi kết quả từng item (`Đã nhớ` / `Cần ôn lại`) và chuyển item kế tiếp.
- Hiển thị trạng thái phiên hoàn tất; backend vẫn là nơi quyết định scheduling, priority và idempotency.
- Giữ content-first: không thêm PageHeader/hero marketing; action nằm ngay cạnh queue.

### Kiểm chứng

- `npm run build` — pass.
- Computer Use: Practice trả lời sai → Ôn tập summary → bắt đầu phiên → ghi kết quả → UI hiển thị “Phiên ôn đã xong” — pass.
- Không thay đổi ownership: Review không chứa knowledge master data và không dùng AI.

## REVIEW-FIX-004 — Review result phát signal cho Progress

### Thay đổi

- Sau khi `RecordResult` cập nhật item, endpoint phát `ReviewEvaluationSignal` sang Progress với event id ổn định theo session/item.
- Review đúng giảm một evidence điểm yếu; Review sai tăng evidence; rule nằm ở Progress projection, không duplicate knowledge data trong Review.
- Retry cùng kết quả vẫn trả kết quả cũ nhưng không làm tăng `ReviewAnswered` lần nữa; retry mâu thuẫn không phát signal.

### Kiểm chứng

- Automated `ProgressStoreTests` — pass: review đúng resolve weak point, review sai giữ `WritingWeak`, duplicate signal idempotent.
- Không dùng AI/RabbitMQ cho signal nội process này.

## REVIEW-FIX-005 — Deterministic scheduler và Hanzi writing regression

### Thay đổi

- Tách `IReviewClock` để InMemory và Marten không phụ thuộc trực tiếp vào system clock trong due query/session/result.
- Tách `ReviewScheduler`: kết quả sai đưa item về due ngay; kết quả đúng đặt lần kế tiếp sau một ngày.
- Hanzi writing tiếp tục dùng `ReviewReason.WritingWeak`; repeated wrong signals merge cùng item và tăng `MistakeCount`.

### Kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 52/52.
- `ReviewStoreTests.Repeated_hanzi_writing_errors_create_writing_weak_item` — pass.
- `ReviewStoreTests.Review_scheduler_uses_injected_clock_for_due_boundary` — pass.
- Cả InMemory fallback và Marten store dùng cùng scheduler contract.

## REVIEW-FIX-006 — Hanzi writing weak item mở đúng practice flow

### Computer Use production QA — 2026-09-22

- Tạo một lượt Hanzi `hanzi-da` không đạt: vẽ sai nét và hoàn tất lượt viết.
- Mở `/app/needs-review`: queue hiển thị item `hanzi-writing · hanzi-da` với reason `WritingWeak`.
- Chọn `Mở nội dung →`: điều hướng chính xác tới `/app/hanzi/hanzi-da/write`, canvas hiển thị `0 / 3 nét` và bắt đầu một lượt luyện mới theo ownership hiện tại.
- Filter `Lọc loại nội dung` hiển thị loại `hanzi-writing` và không làm mất item.

## REVIEW-FIX-007 — Exam incorrect mapping vào Review

### Thay đổi

- `ExamResultSignal` mang thêm `ExamKnowledgeResult` cho các câu đã có knowledge reference.
- Submit Exam gửi signal đồng thời tới Progress và Review; Review chỉ tạo `WrongAnswer` cho câu sai,
  giữ `KnowledgeType/KnowledgeId` của Exam reference và không sở hữu master data.
- `LastSourceEventId` trên `ReviewItem` giúp InMemory/Marten bỏ qua cùng một attempt/question khi
  submit hoặc callback lặp lại.

### Kiểm chứng

- `ReviewStoreTests.Exam_signal_maps_incorrect_knowledge_to_review_once` — pass.
- `scripts/identity-smoke.mjs`: exam trả lời sai → `GET /api/review/mistakes` có
  `vocabulary:vocab-thanks`; submit lần hai không tạo thêm evidence — pass.
