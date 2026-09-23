# Admin Frontend Module

Admin shell riêng, không dùng learner sidebar.

## Navigation

```text
TỔNG QUAN
GIÁO TRÌNH
  Cấp HSK
  Người mới bắt đầu
  Chủ đề / Bài học
KIẾN THỨC
  Pinyin / Thanh điệu
  Từ vựng
  Chữ Hán
  Ngữ pháp
BÀI TẬP & ĐỀ THI
  Ngân hàng câu hỏi
  Đề thi
NỘI DUNG
  Audio
  Truyện
  Video
  Tài liệu
  Công cụ
HỆ THỐNG
  Người dùng
  AI & chi phí
  Nhật ký
```

Menu phải ẩn/disable theo permission thực tế.

## Hanzi admin
Cần xem/edit metadata và stroke data reference/source, nhưng không xây vector editor phức tạp nếu chỉ import dataset.

## Audio admin
Status:
- Chưa tạo;
- Đang xử lý;
- Đã tạo;
- Lỗi.

## Computer Use tests
- [x] permission visibility;
- [x] curriculum content CRUD prototype/production flow;
- [x] audio status/retry;
- [x] extended content publish;
- [x] learner không vào admin.

### Current implementation scope

`frontend/src/views/AdminView.vue` hiện cung cấp admin shell riêng với navigation lọc theo
`content.manage`, `curriculum.manage` và `users.manage`, form import curriculum draft theo
`SyllabusVersion → HSK level → topics JSON`, danh sách HSK draft/published với thao tác publish
level, danh sách Content published/draft cho questions, stories, videos, resources, tools,
status/retry audio, thao tác publish draft, và danh sách user đã provision. Chỉnh sửa sâu từng
node curriculum vẫn là phạm vi chưa triển khai; audio đã có local status/retry flow và Computer
Use evidence, còn provider production nằm ở backend Content.
