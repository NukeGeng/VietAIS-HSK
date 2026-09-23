# Fixed

- PracticeView đã nối `GET /api/practice/questions`, tạo session, submit answer và complete session.
- Computer Use flow: start session → nhập `xin chào` → nhận `Correct` → complete `1/2` — pass.
- Skill-filtered flow: `/app/skills/listening`, `/app/skills/reading`, `/app/skills/writing` nhận
  question đúng type; Computer Use Nghe hoàn tất `1/1` — pass.
- `PRACTICE-FE-BUG-001` — lưu session id theo loại bài trong `sessionStorage`, khôi phục qua
  `GET /api/practice/sessions/{id}`, nạp lại câu trả lời đã gửi và xóa key sau khi complete.
- Route đổi loại practice cũng reload lại question/session state thay vì giữ state của loại trước.
- Computer Use 2026-09-22: `/app/practice/listening` start → trả lời `ở lớp học` → refresh →
  vẫn hiển thị `Correct` và answer → complete `1/1` — pass.
- Computer Use 2026-09-22: `/app/practice/vocabulary` → nhập `xin chào` → `Correct` → complete `1/1`; `/app/practice/tone` → chọn `A 1` cho `mā` → `Correct` → complete `1/1` — pass.
- `bootstrap-pinyin-nihao` đã được thêm vào Content question bank; `/app/pinyin` có CTA `Luyện Pinyin →` vào `/app/practice/pinyin`.
- Computer Use 2026-09-22: `/app/pinyin` → `Luyện Pinyin` → `ni hao` → `Correct` → `Hoàn thành: đúng 1/1 câu.` — pass. Đây là platform-authored fixture; question bank HSK chính thức vẫn pending.
- Practice question renderer đã hỗ trợ option buttons cho tone/Hanzi/ngữ pháp khi Content trả `options`; câu tự do vẫn giữ text input và cùng nút chấm.
- Computer Use 2026-09-22: `/app/practice/tone` start → chọn option `A 1` → `Chấm câu` → UI hiển thị `Correct`; public catalog chỉ hiển thị options, không lộ `acceptedAnswers`.
- Computer Use 2026-09-22: `/app/hanzi/hanzi-da/write` mode `Guided` → vẽ sai → feedback `Điểm bắt đầu chưa đúng vị trí.` → `Làm lại` → vẽ nét đúng → `1 / 3 nét` — pass.
- Computer Use 2026-09-22: viewport `390×844`, `/app/hanzi/hanzi-da/write` → canvas không clipping/overflow, kéo một nét hợp lệ → `1 / 3 nét` và feedback đúng; cuộn xuống vẫn truy cập được action row/provenance — pass.
- `npm run build` — pass.

Question bank chính thức, audio/transcript và mobile 390px regression vẫn chưa được đánh dấu hoàn tất.
