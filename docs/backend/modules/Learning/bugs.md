# Bugs

## LEARNING-BUG-004 — Continue target lấy nhầm bài của track cũ

- Tái hiện bằng HTTP: bắt đầu lesson HSK → chọn Beginner → GET learning/home vẫn trả lesson HSK thay vì `beginner/tones`.
- Nguyên nhân: query ưu tiên CurrentLessonId mà không kiểm tra CurrentTrack.
- Sửa: chọn target theo track đang hoạt động, không xóa state của track khác.

## LEARNING-BUG-003 — Complete chưa bắt đầu vẫn ghi activity

- Reproduce: learner mới POST `/api/learning/beginner/stages/tones/complete` trả 200 và Progress có `beginner-stage-completed`, dù chưa bắt đầu track.
- Nguyên nhân: store trả state hiện tại cho transition bị từ chối; endpoint chỉ kiểm tra null rồi ghi activity. Lesson completion có cùng lỗi.
- Fix: kiểm tra started/current stage trước mutation, trả 409 cho thao tác sai; complete lặp lại trả state cũ và không ghi thêm activity.
- Verification: xem `LEARNING-FIX-007`.

## LEARNING-BUG-001 — Marten append dùng sai event version

- Ngày: 2026-09-22
- Mức độ: High
- Flow: bắt đầu Beginner, sau đó chọn HSK trong cùng learner stream.
- Expected: command nối event mới bằng đúng next 1-based event version của stream.
- Actual: append nhận version hiện tại thay vì version kế tiếp; Marten kiểm tra version trước đó và trả lỗi mismatch.
- Trạng thái: Fixed — xem `LEARNING-FIX-002`; PostgreSQL write → API restart → read đã pass.

## LEARNING-BUG-002 — Beginner stage start lần đầu trả conflict sau khi đã mutate

- Ngày: 2026-09-22
- Mức độ: High
- Flow: learner mở Pinyin lần đầu và bấm `Bắt đầu bước này`.
- Actual: endpoint gọi `StartBeginner` rồi tiếp tục coi `StartBeginnerStage` no-op là lỗi; state đã bị thay đổi nhưng response là conflict, UI hiển thị sai trạng thái.
- Expected: lần đầu bắt đầu Pinyin trả state 200; không được nhảy thẳng sang Thanh điệu.
- Trạng thái: Fixed — xem `LEARNING-FIX-005`; smoke và Computer Use retest đã pass.
