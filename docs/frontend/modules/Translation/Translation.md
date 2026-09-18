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
- [ ] submit không tự gọi feedback UI;
- [ ] request feedback loading/error;
- [ ] feedback render;
- [ ] history if implemented.
