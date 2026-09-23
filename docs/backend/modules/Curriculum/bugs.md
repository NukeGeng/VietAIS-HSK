# Bugs

## CURRICULUM-BUG-005 — Import version mới silently rebind level Id đã tồn tại

- Ngày: 2026-09-23
- Mức độ: High
- Flow: admin import một syllabus version mới dùng lại Id của HSK level từ version cũ.
- Expected: version cũ vẫn giữ nguyên; import bị từ chối nếu Id level không có scope/version duy nhất.
- Actual: store có thể ghi đè `SyllabusVersionId` của level cũ, làm published curriculum bị trỏ
  sang version mới và phá vỡ bất biến của nội dung đã publish.
- Trạng thái: Fixed — xem `CURRICULUM-FIX-010`.

## CURRICULUM-BUG-001 — HSK tree was empty and Learning rejected every lesson

- Ngày: 2026-09-22
- Mức độ: High
- Flow: admin import/publish HSK → learner opens tree and starts a published lesson.
- Expected: Topic/Unit/Lesson persist; public tree shows only published lessons; Learning accepts only those lessons.
- Actual: import stored only syllabus version/level, `GetPublishedTree()` returned an empty array and both stores returned `false` from `IsPublishedLesson()`.
- Trạng thái: Fixed — xem `CURRICULUM-FIX-004`; Marten smoke đã xác minh tree và lesson qua lần restart API.

## CURRICULUM-BUG-002 — Hanzi UI thiếu read contract cho character/stroke reference

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: learner mở Nền tảng → Chữ Hán → detail/stroke order.
- Expected: frontend đọc được character metadata và stroke reference có provenance.
- Actual: route còn là placeholder, backend chưa có endpoint Hanzi.
- Trạng thái: Fixed — xem `CURRICULUM-FIX-005`; catalog hiện là fixture nền tảng, chưa phải HSK dataset chính thức.

## CURRICULUM-BUG-003 — Vocabulary/Grammar route chưa có read catalog thực tế

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: learner mở Nền tảng → Từ vựng/Ngữ pháp → list, filter, detail.
- Expected: frontend nhận được record có HSK mapping, examples và provenance từ Curriculum.
- Actual: route production còn thiếu catalog-specific read contract; UI không thể render dữ liệu
  nền tảng một cách kiểm chứng được.
- Trạng thái: Fixed — xem `CURRICULUM-FIX-006`; fixture vẫn được ghi rõ là platform-authored reference,
  chưa thay thế official HSK dataset.

## CURRICULUM-BUG-004 — Learner chưa có read contract cho lesson Published

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: learner mở HSK level → chọn lesson → xem chi tiết bài học.
- Expected: learner đọc được đúng lesson thuộc level/version Published; lesson Draft hoặc id không tồn tại không được lộ ra.
- Actual: Learning đã có start/complete transition nhưng không có endpoint đọc lesson detail, nên frontend chỉ có thể rơi về placeholder.
- Trạng thái: Fixed — xem `CURRICULUM-FIX-007`.

## CURRICULUM-BUG-006 — Lesson detail thiếu context của cây Published

- Ngày: 2026-09-23
- Mức độ: Medium
- Flow: learner mở lesson detail từ một HSK level cụ thể.
- Actual: endpoint chỉ trả `id/name/status`; frontend không thể biết lesson thuộc level, topic và
  unit nào, dễ dẫn tới nhãn cấp độ hardcode hoặc lệch với sidebar.
- Trạng thái: Fixed — xem `CURRICULUM-FIX-008`.
