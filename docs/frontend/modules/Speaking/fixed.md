# Fixed

## SPEAKING-FE-FIX-001 — Nối flow hội thoại bootstrap vào production frontend

- `SpeakingView` tạo session theo HSK hiện tại của learner, hiển thị transcript/assistant response và gửi từng lượt qua API.
- Có trạng thái bắt đầu, đang gửi, provider unavailable, kết thúc và số lượt đã gửi.
- Không tự gọi provider khi mở trang; không hiển thị thuật ngữ kỹ thuật cho learner.
- Mobile/desktop dùng layout content-first, không thêm hero hoặc breadcrumb dư.

Kiểm chứng:

- `npm run build` — pass.
- Computer Use: route `/app/skills/speaking` desktop/mobile — đã kiểm tra sau khi nối route.
- Computer Use QA 2026-09-22: mở phiên HSK 3, nhập `你好，我每天学习汉语。`, gửi lượt; UI giữ transcript và báo provider sẽ thử lại, sau đó kết thúc phiên thành công — pass.

## SPEAKING-FE-FIX-002 — Đồng bộ context cấp độ learner

- SpeakingView đọc level đã chọn từ Learning/Curriculum trước khi tạo session; copy giới thiệu
  và `hskContext` gửi backend dùng cùng cấp độ, có fallback an toàn khi API chưa sẵn sàng.
- `TranslationView` cũng dùng `selectedExercise.hskContext` trong mô tả thay vì hardcode HSK 3.
- `npm run build` — pass.
