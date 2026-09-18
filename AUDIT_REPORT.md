# AUDIT REPORT — HSK Project Docs v2

## Kết quả rà soát

Bộ tài liệu đã được patch theo navigation/scope mới và kiểm tra cấu trúc.

### Kiểm tra tự động

- Backend business modules: **10/10** có đủ `<Module>.md`, `bugs.md`, `fixed.md`.
- Frontend modules: **14/14** có đủ `<Module>.md`, `bugs.md`, `fixed.md`.
- Markdown files: **130**.
- `TODO` còn lại: **0**.
- File Markdown rỗng: **0**.

## Scope mới đã đồng bộ

- Người mới bắt đầu;
- Pinyin;
- Thanh điệu;
- Chữ Hán;
- Thứ tự nét;
- Luyện viết chữ Hán;
- Nội dung cần ôn;
- Chuỗi ngày học;
- Truyện song ngữ;
- Video học;
- Tài liệu;
- Công cụ.

## Module ownership đã rà soát

Không tạo thêm business module không cần thiết.

```text
Pinyin / Thanh điệu
→ Curriculum + Practice + Learning

Luyện viết chữ Hán
→ Curriculum + Practice + Review + Progress

Chuỗi ngày học
→ Progress

Truyện / Video / Tài liệu / Công cụ
→ Content
```

## Chữ Hán

Đã bổ sung docs riêng cho data/logic luyện viết:

```text
docs/data/04-hanzi-writing-data.md
```

Ownership:

```text
Curriculum: Hanzi + stroke reference/source
Practice: writing attempt + deterministic stroke validation
Review: WritingWeak / item cần ôn
Progress: weakness/mastery aggregation
Frontend Hanzi/Practice: canvas + stroke animation + feedback
```

AI **không** dùng để chấm nét.

## Architecture

Không thay đổi kiến trúc nền:

- Modular Monolith;
- CQRS có chọn lọc;
- Event Sourcing có chọn lọc;
- Marten;
- Wolverine;
- RabbitMQ chỉ cho async/cross-process;
- AI chỉ ở 3 use case đã chốt.

Đã bổ sung mapping navigation → backend module để tránh biến sidebar item thành module riêng.

## Data

Đã bổ sung:

- curriculum model cho Beginner Track;
- Pinyin/Tone foundation data;
- Hanzi stroke data;
- extended content data;
- import validation cho Pinyin/stroke;
- audio pipeline CosyVoice + Storage/CDN.

## Frontend system docs

Đã đồng bộ:

- AppShell/sidebar;
- Learning + Beginner;
- Skills;
- Practice;
- Hanzi writing;
- Review;
- Exam;
- Progress + streak;
- Explore/Mở rộng;
- Admin.

Production frontend vẫn phải bám approved `design-template/`, không redesign.

## Deploy

Đã bổ sung:

```text
docs/deploy/11-storage-cdn.md
```

để cover generated audio/media assets.

## Các quyết định cố ý chưa khóa

Được ghi tại:

```text
docs/decisions/open-decisions.md
```

Gồm:

- auth provider;
- review scheduling algorithm;
- Hanzi stroke dataset/version cuối cùng;
- Hanzi validation tolerance/algorithm;
- speech provider;
- storage/CDN provider;
- hosting topology;
- exact production AI model/version.

Đây không phải TODO thiếu tài liệu; đây là decision cần PoC/duyệt trước khi khóa implementation.
