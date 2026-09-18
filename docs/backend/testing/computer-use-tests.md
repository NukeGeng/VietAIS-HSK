# Computer Use Tests

## Mục đích

Kiểm thử flow thật qua browser/UI sau automated tests.

Computer Use không thay thế Unit/Integration tests.

## Quy trình

```text
Run services
→ mở browser
→ chạy flow documented
→ ghi lỗi vào module bugs.md
→ fix
→ automated retest
→ Computer Use retest
→ cập nhật fixed.md
```

## Critical learner flows

1. Login/Profile;
2. Người mới bắt đầu → Pinyin/Thanh điệu;
3. Lộ trình HSK → Bài học;
4. Từ vựng/Hanzi/Grammar;
5. Hanzi → Thứ tự nét → Luyện viết;
6. Luyện tập → feedback;
7. Ôn tập / Câu làm sai / Nội dung cần ôn;
8. Thi thử → submit → result;
9. Dịch Việt - Trung → optional feedback;
10. Nói - đối thoại;
11. Tiến độ → Điểm yếu → action;
12. Chuỗi ngày học;
13. Mở rộng content sau khi implement.

## Check mỗi flow

- permission;
- route;
- loading/error state;
- refresh/resume;
- persistence;
- text/font/clipping;
- mobile khi flow yêu cầu;
- regression của flow liên quan.
