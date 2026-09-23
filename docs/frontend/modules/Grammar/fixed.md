# Fixed

## GRAMMAR-FE-FIX-002 — Mobile detail typography regression

### Computer Use visual QA — 2026-09-22

- Viewport `390×844`, route `/app/grammar/grammar-zhengzai`.
- Pattern `正在 + V`, cách dùng, ví dụ tiếng Trung/Pinyin/Việt, lỗi thường gặp và CTA bài học liên quan đều hiển thị trọn.
- Không có clipping, overflow ngang hoặc mất dấu tiếng Việt/chữ Hán; provenance cuối card vẫn truy cập được.

## GRAMMAR-FE-FIX-001 — Content-first list/filter/detail

### Thay đổi

- Thay route placeholder bằng `GrammarView.vue` cho list, search, HSK filter và detail.
- Hiển thị pattern, cách dùng tiếng Việt, examples, lỗi thường gặp, lesson liên quan và provenance.
- Giữ action/link gần nội dung; không thêm blocking PageHeader, breadcrumb hoặc mô tả marketing dài.

### Verification

- `npm run build` — pass, Vue type-check và Vite build.
- Computer Use desktop: `/app/grammar` render 4 cấu trúc; `/app/grammar/grammar-zhengzai`
  render pattern, explanation, example, common mistakes và CTA lesson.
- API-backed smoke: list/detail trả đúng `正在 + V` và provenance.

### Chưa hoàn tất

- Mobile 390px visual regression chưa được chạy; không coi module đã hoàn tất QA responsive.

## GRAMMAR-FE-FIX-002 — Topic filter và route-sync

### Thay đổi

- Thêm lọc topic cho danh sách grammar.
- Đồng bộ detail theo `route.params.id` bằng watcher.
- CTA lesson dùng đúng `relatedLessonId`, không còn hard-code `/app/lessons/02`; grammar không có lesson fallback về practice context.

### Verification

- Computer Use: `/app/grammar` hiển thị topic options và 4 cấu trúc.
- Computer Use: click `正在 + V` mở detail, hiển thị explanation, examples, common mistakes và CTA tới `/app/lessons/lesson-02`.
- `npm run build` — pass.

### Chưa hoàn tất

- Mobile 390px visual regression và kiểm tra chi tiết layout trên thiết bị thật vẫn pending.
