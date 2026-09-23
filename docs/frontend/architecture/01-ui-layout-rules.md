# UI Layout Rules

## 1. Core Rule

```text
Content first.
No blocking PageHeader by default.
```

Giao diện trong ứng dụng phải đưa người dùng vào nội dung/chức năng chính càng sớm càng tốt.

Không sử dụng `PageHeader`, `Blocking Header`, `PageHero` hoặc các khối tiêu đề lớn mặc định ở đầu mỗi trang.

---

## 2. Không dùng Blocking Page Header mặc định

Không tự động thêm các khối dạng:

```text
-----------------------------------------
Tên trang rất lớn

Mô tả dài 1–2 dòng

Breadcrumb / metadata / action
-----------------------------------------
```

Ví dụ KHÔNG nên dùng:

```text
TỪ VỰNG

Khám phá và học hàng nghìn từ vựng HSK
theo từng cấp độ và chủ đề.

Trang chủ / Từ vựng
```

Hoặc:

```text
TIẾN ĐỘ HỌC

Theo dõi toàn bộ hành trình học tập của bạn
và khám phá những điểm cần cải thiện.
```

Các block này:

- chiếm nhiều chiều cao;
- lặp lại thông tin sidebar;
- làm giao diện nặng;
- đẩy nội dung chính xuống dưới;
- không phù hợp với app học tập cần thao tác nhanh.

---

## 3. Pattern mặc định cho trang ứng dụng

Ưu tiên:

```text
Tên trang ngắn + action nếu có
        ↓
Filter / context
        ↓
Nội dung chính
```

Ví dụ:

```text
Từ vựng                  [Bộ lọc]

HSK 3 · 420 từ

[danh sách từ]
```

Hoặc:

```text
Ôn tập

Hôm nay cần ôn
12 từ · 5 chữ Hán · 3 ngữ pháp

[Bắt đầu ôn]
```

Không ưu tiên:

```text
PAGE HEADER RẤT LỚN
mô tả dài
breadcrumb
khoảng trắng lớn
↓
nội dung thật
```

---

## 4. Tiêu đề trang

Tiêu đề trang vẫn được phép dùng nhưng phải:

- ngắn;
- gọn;
- nằm trong flow nội dung;
- không tạo một block riêng quá lớn;
- không có mô tả marketing dư thừa;
- không tạo khoảng trắng lớn ở đầu trang;
- không lặp lại context đã có trong sidebar.

---

## 5. Không dùng copy marketing trong app

Không dùng các câu như:

```text
Khám phá hành trình học tiếng Trung toàn diện...
Nâng cao kỹ năng của bạn...
Chinh phục HSK dễ dàng hơn...
```

Trong app chỉ dùng copy chức năng đơn giản.

Ví dụ:

```text
Từ vựng
420 từ · HSK 3

Ngữ pháp
32 cấu trúc

Câu làm sai
18 câu cần xem lại
```

---

## 6. Breadcrumb

Không tự động thêm breadcrumb vào mọi trang.

Chỉ dùng khi:

- page có cấu trúc sâu;
- user dễ mất context;
- detail page thực sự cần quay lại nhiều cấp.

Ví dụ có thể dùng:

```text
Chữ Hán > 学
```

Không cần:

```text
Trang chủ > Học tập > Từ vựng
```

nếu sidebar đã thể hiện rõ vị trí hiện tại.

---

## 7. Action của trang

Action chính phải đặt gần nội dung liên quan.

Ví dụ:

```text
Từ vựng                 [+ Thêm từ]
```

hoặc:

```text
Thi thử                  [Bắt đầu đề]
```

Không tạo một PageHeader lớn chỉ để chứa button.

---

## 8. Các trang áp dụng

Rule này áp dụng cho toàn bộ learner app:

```text
Trang chủ
Lộ trình HSK
Người mới bắt đầu
Bài học
Kỹ năng
Luyện tập
Pinyin
Thanh điệu
Từ vựng
Chữ Hán
Ngữ pháp
Ôn tập
Câu làm sai
Nội dung cần ôn
Thi thử
Tiến độ học
Điểm yếu
Lịch sử học
Chuỗi ngày học
Truyện song ngữ
Video học
Tài liệu
Công cụ
Hồ sơ
Cài đặt
Trợ giúp
```

Và toàn bộ Admin UI.

Mặc định:

```text
NO BLOCKING PAGE HEADER
```

---

## 9. Detail page

Trang chi tiết phải dùng header gọn.

Ví dụ:

```text
← Chữ Hán

学
xué · học · 8 nét

[Nghe] [Luyện viết]
```

Không dùng:

```text
CHI TIẾT CHỮ HÁN

Tìm hiểu chi tiết cách viết, phát âm và sử dụng chữ Hán...
```

---

## 10. List page

Pattern chuẩn:

```text
Tên trang + action
        ↓
Filter / Search
        ↓
List / Grid
```

Ví dụ:

```text
Từ vựng

[HSK 3 ▼] [Chủ đề ▼] [Tìm kiếm...]

[danh sách]
```

---

## 11. Progress page

Không dùng dashboard title block lớn.

Dùng:

```text
Tiến độ học

HSK 3 · 64%

Từ vựng   82%
Chữ Hán   71%
Ngữ pháp  63%
```

Không dùng:

```text
TIẾN ĐỘ HỌC

Theo dõi hành trình học tập và xem mức độ tiến bộ...
```

---

## 12. Admin page

Rule này áp dụng tương tự cho Admin.

Ví dụ:

```text
Từ vựng                  [+ Thêm từ]

[Tìm kiếm] [HSK] [Trạng thái]

Table...
```

Không cần:

```text
QUẢN LÝ TỪ VỰNG

Quản lý toàn bộ dữ liệu từ vựng trong hệ thống...
```

---

## 13. Component rule

Không tạo component mặc định kiểu:

```text
PageHeader
PageHero
BlockingHeader
SectionHero
ContentHeader lớn
```

cho mọi page.

Nếu project đã có component như vậy:

- kiểm tra từng nơi sử dụng;
- chỉ giữ ở nơi thật sự cần;
- không dùng làm wrapper mặc định.

---

## 14. Khi nào được phép dùng header lớn

Chỉ dùng khi có lý do UX rõ ràng:

- public landing page;
- onboarding;
- exam intro trước khi bắt đầu;
- empty state đặc biệt;
- full-screen flow độc lập.

Ngay cả khi đó, header lớn phải là một phần của flow, không phải thói quen thiết kế.

---

## 15. Rule cho agent

Agent không được tự suy luận:

```text
"Trang mới nên có PageHeader để trông chuyên nghiệp."
```

Mặc định phải hiểu:

```text
App page = content-first
```

không phải:

```text
App page = header-first
```

---

## 16. Fix các trang cũ

Khi gặp trang cũ có blocking header:

```text
1. Xác định phần thông tin thật sự cần giữ
2. Giữ tiêu đề ngắn nếu cần
3. Di chuyển action gần nội dung
4. Xóa mô tả dư
5. Xóa breadcrumb không cần thiết
6. Giảm khoảng trắng đầu trang
7. Không redesign toàn bộ page
8. Test lại desktop/mobile
```

---

## 17. HTML/CSS rule

Không tạo mặc định:

```html
<section class="page-header">
  <h1>...</h1>
  <p>...</p>
</section>
```

hoặc:

```html
<div class="page-hero">
```

cho app pages.

Nếu page cần title:

```html
<div class="page-toolbar">
  <h1>Từ vựng</h1>
  <div class="page-actions">...</div>
</div>
```

nên là dạng gọn, content-first.

---

## 18. Responsive

Desktop:

- không để page header chiếm quá nhiều viewport;
- nội dung chính phải xuất hiện sớm;
- không tạo khoảng trống lớn vô nghĩa.

Mobile:

- tiêu đề càng phải gọn;
- action có thể xuống dòng;
- không dùng hero-style header trong app;
- không để breadcrumb dài chiếm nhiều hàng.

---

## 19. Visual QA

Trước khi hoàn thành một trang:

- [ ] Không có blocking PageHeader mặc định.
- [ ] Không có hero-style title block không cần thiết.
- [ ] Không có mô tả marketing.
- [ ] Tiêu đề ngắn, rõ.
- [ ] Action ở gần nội dung liên quan.
- [ ] Sidebar đã cung cấp đủ context.
- [ ] Không có breadcrumb thừa.
- [ ] Nội dung chính xuất hiện sớm.
- [ ] Không lãng phí chiều cao viewport.
- [ ] Desktop/mobile đều ổn.
- [ ] Không clipping hoặc overflow.
- [ ] Không mất dấu tiếng Việt.
- [ ] Chữ Hán/Pinyin hiển thị đúng.

---

# Final Rule

```text
Content first.
No blocking PageHeader by default.
```
