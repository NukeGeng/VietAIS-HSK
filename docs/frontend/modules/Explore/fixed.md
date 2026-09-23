# Fixed

## EXPLORE-FE-FIX-003 — Detail content-first

- Bỏ header “Chi tiết truyện” và kicker lặp trong phần nội dung khi đã tải detail; tiêu đề truyện là h1, giữ link quay lại.
- Computer Use ở 390×844 và 1440×900: story-classroom hiển thị trọn chữ Hán/Pinyin/nghĩa Việt, không bị che/cắt trong trạng thái ổn định. Error state cùng hai viewport hiển thị thông báo và nút Thử lại đầy đủ.
- Build pass. Kết quả này chỉ áp dụng detail truyện và error state đã kiểm tra, không thay thế QA mọi trang Explore.

## EXPLORE-FE-FIX-002 — Phân biệt lỗi API với nội dung thật

- Loại bỏ fallback item mẫu khi Content API lỗi, thêm error state và Thử lại. Request cũ không ghi đè state của route mới.
- Build Vue/TypeScript pass.
- Computer Use: detail `/app/stories/nonexistent-qa` hiện lỗi, bấm Thử lại vẫn giữ lỗi đúng; chuyển về danh sách tải được record API, không giữ lỗi cũ.
- Thay thế chính sách static fallback trong FIX-001 bên dưới. Responsive 1440/390 và race test có delay vẫn cần bổ sung.

- `EXPLORE-FE-FIX-001` — thay dữ liệu tĩnh của Story/Video/Resource/Tool bằng Content API published-only; thêm list/detail route cho Story/Video/Resource và giữ static fallback khi API không sẵn sàng.
- Computer Use QA 2026-09-22: `/app/stories` hiển thị story published, click mở `/app/stories/story-classroom` với chữ Trung, Pinyin và nghĩa Việt; `/app/video`, `/app/resources`, `/app/tools` hiển thị record published — pass.

## EXPLORE-FE-FIX-004 — Mobile detail regression

### Computer Use visual QA — 2026-09-22

- Viewport `390×844`.
- Story `/app/stories/story-classroom`: tiêu đề, chữ Hán, Pinyin và nghĩa Việt hiển thị trọn, không clipping.
- Video `/app/video/video-classroom`: mô tả, transcript và nút mở tài nguyên nằm trong card, không overflow.
- Resource `/app/resources/resource-hsk3-grammar`: tiêu đề, mô tả và CTA mở tài nguyên hiển thị đầy đủ.
- Tools `/app/tools`: danh sách tool published hiển thị đúng; route `Tra cứu nhanh` vẫn hoạt động.
- Desktop đã có regression detail story; mobile pass bổ sung cho toàn bộ detail routes hiện có.
