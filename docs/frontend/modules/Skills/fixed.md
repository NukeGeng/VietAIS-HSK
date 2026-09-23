# Fixed

## SKILLS-FE-FIX-001 — Route kỹ năng đi vào module đúng

- Nghe/Đọc/Viết vẫn đi qua practice content-first.
- Nói - đối thoại đi vào `SpeakingView`.
- Dịch Việt - Trung đi vào `TranslationView`.
- Sidebar vẫn chỉ có nhóm Kỹ năng và dropdown; không tạo thêm page tổng quan thừa.

Kiểm chứng:

- `npm run build` — pass.
- Computer Use mobile drawer giữ active child sau khi chuyển sang Speaking/Translation.

## SKILLS-FE-FIX-002 — Nghe/Đọc/Viết đi vào Practice theo đúng skill

- `/app/skills/listening`, `/app/skills/reading`, `/app/skills/writing` dùng `PracticeView`.
- API question catalog được gọi với `type` tương ứng; không trộn câu giữa các kỹ năng.
- UI hiển thị nhãn tiếng Việt và giữ content-first layout, không thêm PageHeader blocking.

Kiểm chứng:

- `npm run build` — pass.
- Computer Use: Nghe bắt đầu → trả lời đúng → nhận `Correct` → complete `1/1` — pass.
- Mobile 390px visual regression và audio/transcript asset vẫn pending.
