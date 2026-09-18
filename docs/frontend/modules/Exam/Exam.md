# Exam Frontend Module

Visible name: `Thi thử`.

## Screens / routes

```text
/app/exams
/app/exams/:examId
/app/exam-attempts/:attemptId
/app/exam-attempts/:attemptId/result
```

## Danh sách đề

Hiển thị ngắn gọn:
- HSK;
- tên đề;
- thời lượng;
- số phần;
- trạng thái/lần làm gần nhất khi có.

## Chi tiết đề
- cấu trúc;
- thời gian;
- hướng dẫn;
- CTA `Bắt đầu` / `Tiếp tục`.

## Màn thi

Focus layout, giảm xao nhãng.

```text
Header tối giản
├── Thời gian
├── Tiến độ
└── Nộp bài

Question navigation (nếu cần)
Main question
Previous / Next
```

Không để sidebar learner full-size nếu làm giảm diện tích/độ tập trung.

## Result

- tổng kết;
- điểm/kết quả theo phần;
- objective result;
- trạng thái phần tự luận đang chấm nếu AI async;
- câu sai;
- điểm yếu;
- nội dung nên ôn.

Không gọi mọi điểm là `AI score`.

## States

- loading exam;
- active;
- connection/problem saving answer;
- submitted;
- subjective grading pending;
- scored;
- error/retry.

## Computer Use tests

- [ ] start exam;
- [ ] answer + next/previous;
- [ ] refresh/resume;
- [ ] submit confirm;
- [ ] double submit không tạo lỗi UX;
- [ ] objective result hiển thị;
- [ ] pending subjective grading state;
- [ ] final result;
- [ ] mobile/focus layout.
