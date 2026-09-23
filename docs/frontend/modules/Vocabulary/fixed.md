# Fixed

## VOCABULARY-FE-FIX-001 — Content-first list/filter/detail

### Thay đổi

- Thay route placeholder bằng `VocabularyView.vue` cho list, search, HSK filter và detail.
- Hiển thị chữ Hán, Pinyin, nghĩa, loại từ, topic, examples, chữ cấu tạo và provenance.
- Giữ layout gọn theo rule `Content first`; không thêm blocking PageHeader, breadcrumb hoặc copy marketing.

### Verification

- `npm run build` — pass, Vue type-check và Vite build.
- Computer Use desktop: `/app/vocabulary` render 4 record; `/app/vocabulary/vocab-anpai` render
  detail `安排`, Pinyin, ví dụ và provenance.
- API-backed smoke: filter `安排` + `HSK 3` trả đúng record.

### Chưa hoàn tất

- Mobile 390px visual regression chưa được chạy; không coi module đã hoàn tất QA responsive.

## VOCABULARY-FE-FIX-002 — Audio, topic filter và route-sync

### Thay đổi

- Thêm lọc topic bên cạnh HSK filter.
- Thêm audio control dùng Web Speech API của trình duyệt cho từ và câu ví dụ; không gọi AI/provider ngoài.
- Đồng bộ detail theo `route.params.id` bằng watcher để click list → detail không giữ màn hình cũ.

### Verification

- Computer Use: `/app/vocabulary` hiển thị topic options `Công việc`, `Học tập`, `Sinh hoạt`; chọn `Công việc` còn 2 từ.
- Computer Use: click `学习` mở `/app/vocabulary/vocab-xuexi`, hiển thị nút `🔊 Nghe phát âm` và `Nghe câu`.
- `npm run build` — pass.

### Chưa hoàn tất

- Kiểm tra chi tiết trên thiết bị thật vẫn ngoài phạm vi browser QA; viewport 390px đã được kiểm tra ở `VOCABULARY-FE-FIX-003`.

## VOCABULARY-FE-FIX-003 — Mobile glyph không wrap

### Thay đổi

- Khóa `.knowledge-glyph` bằng `white-space: nowrap` và giảm cỡ chữ riêng ở mobile để từ một hoặc hai chữ Hán không xuống dòng trong ô cố định.
- Giữ nguyên kích thước card, hierarchy và audio control; chỉ sửa overflow responsive.

### Computer Use visual QA — 2026-09-22

- Viewport `390×844`, route `/app/vocabulary/vocab-anpai`.
- Xác nhận `安排` nằm trên một dòng trong glyph box, nút `Nghe phát âm` không còn bị che và phần Ví dụ/Chữ cấu tạo vẫn truy cập được.

## VOCABULARY-FE-FIX-004 — Lọc theo catalog và trạng thái detail

### Thay đổi

- Selector HSK lấy danh sách cấp độ duy nhất từ catalog đang hiển thị, sắp theo số; không ghi cứng HSK 1/3.
- Detail id không có trong catalog hiển thị thông báo ngắn và link về danh sách thay vì render nhầm list.
- Lỗi Speech API có thông báo riêng tại list/detail; chỉ lỗi tải API mới thay nội dung bằng error card.

### Verification — 2026-09-24

- Computer Use 1440×900: list có bốn record, không tràn ngang; heading và filter ở ngay đầu nội dung.
- Computer Use 390×844: list và detail `安排` hiển thị đủ glyph, Pinyin, nghĩa, ví dụ và nút nghe; không tràn ngang.
- Computer Use 390×844: `/app/vocabulary/not-found-qa` hiện “Không tìm thấy từ vựng này.” và link quay lại, không còn bốn card list.
- Computer Use 390×844: chọn `HSK 3` còn `安排`, `经验`; selector hiện các cấp từ dữ liệu fixture (`HSK 1`, `HSK 3`). Chưa có dữ liệu HSK 2 trong catalog hiện tại để kiểm chứng bằng UI.
- Vue type-check và Vite build pass. Nhánh trình duyệt thiếu Speech API được sửa theo code path; runtime QA trên browser không hỗ trợ API này vẫn cần thực hiện trước khi đóng hẳn bug 006.
