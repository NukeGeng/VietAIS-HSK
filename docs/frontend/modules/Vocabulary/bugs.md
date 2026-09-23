# Bugs

## VOCABULARY-FE-BUG-001 — Route Từ vựng từng dùng placeholder chung

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: mở `/app/vocabulary`, lọc HSK, mở detail.
- Actual: route chưa có list/detail UI gắn với Curriculum read contract.
- Trạng thái: Fixed — xem `VOCABULARY-FE-FIX-001`.

QA mobile list/detail được ghi ở `VOCABULARY-FE-FIX-003` và `VOCABULARY-FE-FIX-004`.
Kiểm tra runtime trên trình duyệt thiếu Speech API vẫn là hạng mục QA riêng của bug 006.

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

## VOCABULARY-FE-BUG-004 — Bộ lọc cấp độ ghi cứng HSK 1 và HSK 3

- Ngày: 2026-09-24
- Mức độ: Medium
- Flow: nạp danh sách từ vựng có thêm cấp HSK khác.
- Expected: bộ lọc hiển thị mọi cấp độ hiện có trong catalog đã tải.
- Actual: selector chỉ có `Tất cả`, `HSK 1`, `HSK 3` dù dữ liệu có thể thay đổi sau import.
- Trạng thái: Fixed — xem `VOCABULARY-FE-FIX-004`.

## VOCABULARY-FE-BUG-005 — Detail không tồn tại hiển thị nhầm danh sách

- Ngày: 2026-09-24
- Mức độ: Medium
- Flow: mở `/app/vocabulary/not-found-qa`.
- Expected: thông báo không tìm thấy từ và đường quay lại danh sách.
- Actual: URL vẫn là detail nhưng màn hình hiển thị đủ bốn từ của list; đã tái hiện bằng Computer Use ở 390px.
- Trạng thái: Fixed — xem `VOCABULARY-FE-FIX-004`.

## VOCABULARY-FE-BUG-006 — Trình duyệt thiếu Speech API làm mất nội dung

- Ngày: 2026-09-24
- Mức độ: Medium
- Flow: bấm nút nghe trên trình duyệt không có `speechSynthesis`.
- Expected: thông báo âm thanh không khả dụng ở ngay màn hiện tại.
- Actual theo nhánh code: `error` chung với lỗi tải danh sách khiến toàn bộ list/detail bị thay bằng error card.
- Trạng thái: Code fixed — xem `VOCABULARY-FE-FIX-004`; runtime QA trên trình duyệt thiếu Speech API còn mở.
