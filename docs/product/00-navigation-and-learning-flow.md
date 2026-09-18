# Product Navigation & Learning Flow

## Sidebar đã chốt

```text
TỔNG QUAN
├── Trang chủ
├── Lộ trình HSK
└── Người mới bắt đầu

HỌC TẬP
├── Bài học
├── Kỹ năng
│   ├── Nghe
│   ├── Đọc
│   ├── Viết
│   ├── Nói - đối thoại
│   └── Dịch Việt - Trung
└── Luyện tập
    ├── Từ vựng
    ├── Chữ Hán
    ├── Ngữ pháp
    ├── Nghe
    ├── Đọc
    └── Viết

NỀN TẢNG
├── Pinyin
├── Thanh điệu
├── Từ vựng
├── Chữ Hán
│   ├── Danh sách chữ
│   ├── Thứ tự nét
│   ├── Luyện viết
│   └── Từ liên quan
└── Ngữ pháp

ÔN TẬP & KIỂM TRA
├── Ôn tập
├── Câu làm sai
├── Nội dung cần ôn
└── Thi thử

TIẾN ĐỘ
├── Tiến độ học
├── Điểm yếu
├── Lịch sử học
└── Chuỗi ngày học

MỞ RỘNG
├── Truyện song ngữ
├── Video học
├── Tài liệu
└── Công cụ

TÀI KHOẢN
├── Hồ sơ
├── Cài đặt
└── Trợ giúp
```

## Không đồng nhất sidebar với backend module

Sidebar là information architecture cho người dùng.

Backend module là boundary nghiệp vụ.

Ví dụ:

```text
NỀN TẢNG → Chữ Hán → Luyện viết
```

không tạo `HanziWritingModule`.

Nó dùng:

```text
Curriculum → dữ liệu nét
Practice   → attempt / validation
Review     → nội dung cần ôn
Progress   → tiến độ / điểm yếu
```

## Flow người mới bắt đầu

```text
Pinyin
→ Thanh điệu
→ Âm đầu / Vần
→ Chữ Hán cơ bản
→ Nét cơ bản
→ Bài làm quen
→ HSK 1 khi sẵn sàng
```

## Flow HSK

```text
Chọn HSK
→ Bài học theo Chủ đề / Unit
→ Luyện tập
→ Ôn tập
→ Thi thử
→ Điểm yếu
→ Học/luyện lại
```

## Khác nhau giữa Nền tảng và Luyện tập

`Nền tảng → Từ vựng`:
- học;
- tra cứu;
- nghe;
- xem ví dụ.

`Luyện tập → Từ vựng`:
- làm bài;
- kiểm tra nhớ từ;
- nhận feedback.

Tương tự với Chữ Hán và Ngữ pháp.
