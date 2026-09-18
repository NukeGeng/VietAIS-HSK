# PATCH SUMMARY — Navigation / Beginner / Hanzi Writing Scope

Bản này rà soát và đồng bộ lại system docs sau khi chốt sidebar/application scope mới.

## Không thay đổi kiến trúc nền

Giữ nguyên:

- Modular Monolith;
- ASP.NET Core;
- Marten + PostgreSQL;
- Wolverine;
- RabbitMQ chỉ cho async/cross-process;
- Event Sourcing có chọn lọc;
- Vue 3 cho production frontend sau khi design-template được duyệt;
- AI chỉ ở 3 use case đã chốt.

## Scope được bổ sung / làm rõ

- Người mới bắt đầu;
- Pinyin;
- Thanh điệu;
- Chữ Hán có thứ tự nét và luyện viết;
- Nội dung cần ôn;
- Chuỗi ngày học;
- Truyện song ngữ;
- Video học;
- Tài liệu;
- Công cụ;
- Learner profile có HSK hiện tại / mục tiêu / múi giờ / thiết lập học;
- mapping sidebar -> frontend -> backend module.

## Nguyên tắc module

Không tạo module riêng cho `Pinyin`, `Tone`, `Story`, `Video`, `Streak`.

Chúng thuộc các module hiện có:

- Curriculum: Pinyin, thanh điệu, Hanzi, beginner foundation;
- Learning: beginner path + HSK path;
- Practice: luyện Pinyin, tone, Hanzi writing và kỹ năng;
- Review: nội dung cần ôn / câu sai;
- Progress: tiến độ / điểm yếu / streak / lịch sử;
- Content: truyện, video, tài liệu, công cụ, audio, question bank.

## Chữ Hán

Luồng sở hữu được chốt:

```text
Curriculum
→ dữ liệu chữ + dữ liệu nét

Practice
→ attempt luyện viết + kiểm tra nét

Review
→ đưa chữ yếu/sai vào hàng đợi ôn

Progress
→ tổng hợp mức độ nắm vững chữ
```

AI không dùng để chấm nét chữ Hán.
