# Translation Frontend Module

Visible name: `Dịch Việt - Trung`.

## Flow

```text
Câu tiếng Việt
→ textarea tiếng Trung
→ Kiểm tra / lưu attempt
→ Đáp án tham khảo
→ Nhận góp ý (optional AI)
```

AI chỉ gọi khi user bấm `Nhận góp ý`.

## Feedback labels
- Ý nghĩa;
- Ngữ pháp;
- Dùng từ;
- Độ tự nhiên;
- Gợi ý sửa.

## Computer Use tests
- [x] submit không tự gọi feedback UI;
- [x] request feedback loading/error;
- [x] provider unavailable hiển thị lỗi an toàn và giữ attempt;
- [x] history render trên desktop/mobile.
