# Bugs

## VOCABULARY-FE-BUG-001 — Route Từ vựng từng dùng placeholder chung

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: mở `/app/vocabulary`, lọc HSK, mở detail.
- Actual: route chưa có list/detail UI gắn với Curriculum read contract.
- Trạng thái: Fixed — xem `VOCABULARY-FE-FIX-001`.

Không còn bug mở đã biết. Audio control và mobile visual regression vẫn là hạng mục QA chưa hoàn tất,
không được đánh dấu fixed cho tới khi có test tương ứng.

## VOCABULARY-FE-BUG-002 — Đổi list → detail không đồng bộ route param

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: mở `/app/vocabulary`, bấm một record.
- Actual: URL đổi sang detail nhưng component vẫn giữ list vì chỉ đọc `route.params.id` trong `onMounted`.
- Trạng thái: Fixed — xem `VOCABULARY-FE-FIX-002`.

## VOCABULARY-FE-BUG-003 — Glyph từ hai chữ bị wrap trên mobile

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: mở `/app/vocabulary/vocab-anpai` ở viewport 390px.
- Expected: glyph `安排` nằm gọn trong ô và không che nút audio.
- Actual: cỡ chữ desktop giữ nguyên làm chữ xuống dòng trong ô 92px, tràn xuống vùng audio.
- Trạng thái: Fixed — xem `VOCABULARY-FE-FIX-003`.
