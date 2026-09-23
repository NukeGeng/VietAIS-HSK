# Bugs

## REVIEW-BUG-001 — Review queue và phiên ôn chỉ tồn tại trong RAM

- Ngày: 2026-09-22
- Mức độ: High
- Flow: làm sai câu trong Practice, mở Ôn tập, ghi kết quả rồi API restart.
- Expected: ReviewItem và kết quả của ReviewSession được lưu, replay sau restart, không lộ item của learner khác.
- Actual: `InMemoryReviewStore` là store duy nhất; Practice wrong signal và review result biến mất sau khi process dừng.
- Trạng thái: Fixed — xem `REVIEW-FIX-002`; PostgreSQL smoke đã đi qua hai lần restart.

## REVIEW-BUG-002 — Production UI chưa có thao tác bắt đầu và ghi kết quả phiên ôn

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: learner mở Ôn tập/Câu làm sai/Nội dung cần ôn.
- Expected: learner thấy item đến hạn, bắt đầu một ReviewSession, ghi “Đã nhớ” hoặc “Cần ôn lại”, rồi thấy trạng thái hoàn tất.
- Actual: UI chỉ render summary/list; không gọi `POST /api/review/sessions` hoặc `POST /api/review/sessions/{id}/results`.
- Trạng thái: Fixed — xem `REVIEW-FIX-003`.

## REVIEW-BUG-003 — Due boundary phụ thuộc system clock trực tiếp

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: kiểm tra item đến hạn hoặc replay review result ở thời điểm boundary.
- Expected: scheduler có thể kiểm chứng deterministic với cùng timestamp và hoạt động giống nhau ở InMemory/Marten.
- Actual: store gọi trực tiếp `DateTimeOffset.UtcNow`, khiến test boundary phụ thuộc thời điểm chạy.
- Trạng thái: Fixed — xem `REVIEW-FIX-005`; dùng `IReviewClock` + test boundary.
