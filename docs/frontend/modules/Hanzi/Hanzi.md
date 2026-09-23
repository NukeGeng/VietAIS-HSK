# Hanzi Frontend Module

## Mục đích

Cung cấp phần Nền tảng → Chữ Hán và giao diện học/tra cứu chữ, thứ tự nét và luyện viết.

## Screens / routes

```text
/app/hanzi                 Danh sách chữ
/app/hanzi/:id             Chi tiết chữ
/app/hanzi/:id/strokes     Thứ tự nét
/app/hanzi/:id/write       Luyện viết
/app/hanzi/related         Từ liên quan theo catalog
```

`Từ liên quan` có thể là tab/section trong detail thay vì page độc lập nếu UX tốt hơn. Khi
được expose trong sidebar, route này phải dùng cùng catalog read contract, không dùng page
placeholder hoặc copy marketing.

## Danh sách chữ

Hiển thị:
- Hanzi;
- Pinyin;
- nghĩa;
- HSK;
- số nét;
- trạng thái học/ôn khi có.

## Chi tiết

- chữ lớn;
- Pinyin;
- nghĩa;
- bộ thủ;
- số nét;
- audio nếu có;
- từ liên quan;
- CTA xem nét / luyện viết.

## Thứ tự nét

- animation;
- next/previous/replay nếu cần;
- từng stroke rõ;
- không biến thành canvas practice nếu user chỉ đang xem.

## Luyện viết

UI cần đủ không gian cho canvas.

Modes:
- Theo hướng dẫn;
- Tô theo mẫu;
- Tự nhớ viết.

Feedback:
- nét đúng;
- nét cần thử lại;
- order/direction issue khi validator trả được.

Không dùng điểm số giả/AI score.

## Components

- HanziCard;
- HanziDetailHeader;
- StrokeAnimator;
- HanziWritingCanvas;
- WritingModeSelector;
- StrokeResultList;
- RelatedWords.

## Computer Use tests

- [x] filter/search Hanzi;
- [x] detail renders Chinese/Pinyin đúng;
- [x] stroke animation;
- [x] Guided writing;
- [x] Recall writing;
- [x] retry flow;
- [x] responsive/mobile canvas.
