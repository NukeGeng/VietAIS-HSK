# Fixed

## TRANSLATION-FE-FIX-001 — Nối flow dịch Việt - Trung vào production frontend

- `TranslationView` tải exercise catalog và history từ API.
- Learner nhập câu và lưu attempt trước; không tự gọi feedback.
- Nút `Nhận góp ý` mới gọi feedback endpoint, render đủ 5 nhóm góp ý và giữ attempt khi provider unavailable.
- Mobile/desktop dùng layout content-first, không thêm hero hoặc breadcrumb dư.

Kiểm chứng:

- `npm run build` — pass.
- Computer Use: route `/app/skills/translation` desktop/mobile — đã kiểm tra sau khi nối route.
- Mô tả màn dịch dùng `selectedExercise.hskContext`, không còn hardcode HSK 3 khi catalog trả
  exercise thuộc cấp độ khác.
