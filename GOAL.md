# GOAL.md — HSK Learning Platform

## 1. Mục tiêu

Xây dựng nền tảng học HSK 3.0 với hai cửa vào học tập:

```text
Người mới bắt đầu
        hoặc
Lộ trình HSK
        ↓
Học bài
        ↓
Luyện tập
        ↓
Ôn tập
        ↓
Thi thử
        ↓
Phân tích điểm yếu
        ↓
Học lại phần cần cải thiện
```

Hệ thống phải:

- bám dữ liệu HSK 3.0 đã xác định;
- có nền tảng Pinyin, thanh điệu, từ vựng, chữ Hán và ngữ pháp;
- có luyện viết chữ Hán theo dữ liệu nét;
- dùng tiếng Việt đơn giản trong UI;
- không lạm dụng AI;
- không over-engineering;
- backend theo Modular Monolith;
- dùng Marten + Wolverine + RabbitMQ có chọn lọc;
- production frontend dùng Vue 3;
- `design-template/` HTML/CSS là mẫu tham chiếu panda để duyệt visual trước khi production frontend triển khai;
  đây không phải production frontend và không được dùng như một runtime UI thứ hai.

---

## 2. Phạm vi sản phẩm

### Tổng quan

- Trang chủ;
- Lộ trình HSK;
- Người mới bắt đầu.

### Học tập

- Bài học;
- Kỹ năng: Nghe, Đọc, Viết, Nói - đối thoại, Dịch Việt - Trung;
- Luyện tập: Từ vựng, Chữ Hán, Ngữ pháp, Nghe, Đọc, Viết.

### Nền tảng

- Pinyin;
- Thanh điệu;
- Từ vựng;
- Chữ Hán: danh sách chữ, thứ tự nét, luyện viết, từ liên quan;
- Ngữ pháp.

### Ôn tập & kiểm tra

- Ôn tập;
- Câu làm sai;
- Nội dung cần ôn;
- Thi thử.

### Tiến độ

- Tiến độ học;
- Điểm yếu;
- Lịch sử học;
- Chuỗi ngày học.

### Mở rộng

- Truyện song ngữ;
- Video học;
- Tài liệu;
- Công cụ.

Các nội dung mở rộng thuộc scope hệ thống nhưng triển khai sau các flow học cốt lõi.

---

## 3. Stack chính

### Frontend production

```text
Vue 3
TypeScript
Vite
Vue Router
Pinia
```

### Backend

```text
ASP.NET Core
Marten
Wolverine
RabbitMQ
PostgreSQL
```

### AI / Worker

```text
DeepSeek
CosyVoice
Speech / STT / TTS provider phù hợp
```

AI chỉ dùng ở:

```text
1. Chấm/góp ý phần tự luận trong bài test lớn
2. Góp ý Dịch Việt - Trung
3. Nói - đối thoại
```

Không dùng AI cho:

- chấm trắc nghiệm;
- SRS/ôn tập;
- progress rule;
- chấm nét chữ Hán;
- phát âm từ vựng đã sinh sẵn;
- CRUD nội dung.

---

## 4. Kiến trúc module

Business modules:

```text
Identity
Curriculum
Learning
Practice
Review
Exam
Translation
Speaking
Progress
Content
```

Không tạo module riêng chỉ vì sidebar có một mục riêng.

Ví dụ:

```text
Pinyin / Thanh điệu  -> Curriculum + Practice
Luyện viết chữ Hán   -> Curriculum + Practice + Review + Progress
Chuỗi ngày học       -> Progress
Truyện / Video       -> Content
```

---

## 5. Thứ tự đọc tài liệu bắt buộc

Không code bằng cách chỉ đọc GOAL rồi tự suy đoán.

Khi bắt đầu một task/module:

```text
1. GOAL.md
2. WORKFLOW.md
3. docs/product/
4. docs/architecture/
5. docs/data/ liên quan
6. docs/security/ liên quan
7. docs/backend/api/
8. docs/backend/building-blocks/ liên quan
9. docs/backend/modules/<Module>/
10. docs/backend/workers/ nếu module cần worker
11. docs/frontend/ khi làm production frontend
12. docs/deploy/ khi làm deploy
```

Chỉ đọc module/dependency thực sự liên quan đến task hiện tại.

---

## 6. Thứ tự triển khai backend

```text
1. Identity
2. Curriculum
3. Learning
4. Practice
5. Review
6. Exam
7. Translation
8. Speaking
9. Progress
10. Content
```

Lý do:

- Curriculum phải có dữ liệu nền trước khi Practice/Learning dùng;
- Review phụ thuộc kết quả Practice;
- Progress tổng hợp từ nhiều module;
- Content mở rộng triển khai sau core learning flow.

Không làm nhiều module lớn song song nếu chưa hoàn thành test module nền phụ thuộc.

---

## 7. Quy trình cho MỖI module

### Bước 1 — Đọc docs module

Ví dụ Exam:

```text
docs/backend/modules/Exam/Exam.md
docs/backend/modules/Exam/bugs.md
docs/backend/modules/Exam/fixed.md
```

Đọc thêm product/architecture/data/building-blocks có liên quan.

### Bước 2 — Xác nhận scope

Trước khi code phải trả lời được:

```text
Module phục vụ việc gì?
Actor nào dùng?
Permission nào?
Chức năng nào?
Business rule/invariant nào?
Document/Event/Read Model nào?
Command/Query nào?
Projection nào?
API nào?
RabbitMQ có cần không?
AI có cần không?
Computer Use flow nào phải test?
```

Nếu docs chưa trả lời được thì cập nhật docs trước.

### Bước 3 — Implement đúng scope

Không:

- thêm architecture mới;
- thêm dependency không cần;
- sửa module ngoài scope;
- event-source mọi dữ liệu;
- dùng RabbitMQ cho CRUD/query bình thường;
- dùng AI cho logic deterministic;
- thay đổi approved HTML design khi implement frontend.

### Bước 4 — Automated tests

Bắt buộc khi phù hợp:

```text
Unit test
Integration test
Permission test
Architecture test
```

Build thành công không đồng nghĩa task hoàn thành.

### Bước 5 — Computer Use test

Khi module có UI flow:

```text
Run application
→ mở browser
→ chạy flow thật
→ kiểm tra state / permission / error / refresh / persistence
```

### Bước 6 — Ghi bug

Lỗi phải ghi vào:

```text
docs/backend/modules/<Module>/bugs.md
```

Không sửa âm thầm rồi bỏ lịch sử.

### Bước 7 — Fix + retest

Sau fix:

```text
Automated tests
→ Computer Use retest
→ Regression test
```

### Bước 8 — fixed.md

Bug đã xác nhận fix phải có checklist trong:

```text
docs/backend/modules/<Module>/fixed.md
```

Không xóa bug khỏi `bugs.md`.

---

## 8. Module completion checklist

```text
[ ] Scope đúng docs
[ ] Build pass
[ ] Unit test pass
[ ] Integration test pass
[ ] Permission test pass
[ ] Computer Use flow pass
[ ] Không còn Critical bug
[ ] Không còn High bug
[ ] bugs.md cập nhật
[ ] fixed.md cập nhật
[ ] Docs phản ánh implementation hiện tại
```

---

## 9. Frontend

`frontend/` là frontend production duy nhất. Production frontend chỉ bắt đầu sau khi màn tương ứng
được duyệt trong `design-template/`; Vue tái hiện approved template và không tạo thêm một bản runtime
trong `design-template/`.

Frontend module cũng có:

```text
<Module>.md
bugs.md
fixed.md
```

---

## 10. Deploy

Không deploy theo trí nhớ.

Đọc `docs/deploy/` theo môi trường:

```text
local
development
staging
production
```

Checklist tối thiểu:

- PostgreSQL/Marten;
- RabbitMQ;
- backend;
- frontend;
- workers;
- object storage/audio;
- environment variables;
- health checks;
- backup;
- monitoring;
- rollback.

---

## 11. Definition of Done hệ thống

Không hoàn thành chỉ vì API/UI chạy.

Hoàn thành khi:

```text
Docs khớp implementation
Automated tests pass
Computer Use critical flows pass
Bug history đầy đủ
Fixed checklist đầy đủ
Staging deploy pass
Regression pass
```
