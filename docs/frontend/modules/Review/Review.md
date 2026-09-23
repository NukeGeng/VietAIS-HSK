# Review Frontend Module

## Screens

```text
/app/review
/app/mistakes
/app/needs-review
```

Visible labels:
- Ôn tập;
- Câu làm sai;
- Nội dung cần ôn.

Không dùng `SRS` làm menu chính.

## Ôn tập
- summary hôm nay;
- counts theo loại;
- `Bắt đầu ôn`.

## Nội dung cần ôn
Có thể gồm:
- từ;
- Hanzi;
- Hanzi viết yếu;
- grammar;
- câu sai.

Mỗi item phải có action phù hợp.

## Computer Use tests
- [x] review summary;
- [x] needs-review list hiển thị item sau Practice trả lời sai;
- [x] Hanzi weak → mở đúng writing flow;
- [x] mistakes → làm lại;
- [x] empty state.
- [x] needs-review filter theo loại nội dung.
