# AI Building Block

## Approved runtime use cases

Chỉ:

```text
1. Exam subjective grading / feedback
2. Dịch Việt - Trung feedback theo yêu cầu
3. Nói - đối thoại
```

Không dùng AI cho:
- objective grading;
- SRS/review scheduling;
- Hanzi stroke grading;
- progress rules;
- curriculum lookup;
- generic chatbot.

## Responsibilities

- provider/model abstraction vừa đủ;
- timeout/cancellation;
- structured output validation;
- retry giới hạn;
- usage/cost logging;
- correlation/user/module metadata;
- safe fallback khi provider lỗi.

## Cost control

- không gọi AI khi deterministic logic đủ;
- translation chỉ gọi khi learner yêu cầu feedback;
- exam gom subjective grading hợp lý thay vì call cho mọi objective item;
- speaking per-turn phải có session/product limits theo policy;
- generated content runtime không phải default.
