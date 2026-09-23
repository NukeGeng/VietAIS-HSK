# Frontend Overview

`frontend/` là production UI duy nhất. Production frontend chỉ bắt đầu sau khi page tương ứng đã
được duyệt trong `design-template/`; template chỉ là reference/QA fixture chạy độc lập trên cổng
preview và không được import, build hoặc deploy cùng Vue.

Stack:

- Vue 3;
- TypeScript;
- Vite;
- Vue Router;
- Pinia.

## Nguyên tắc

- approved HTML/CSS trong `design-template/` là visual reference để đối chiếu;
- chỉ `frontend/` được build/deploy làm runtime UI, không duy trì implementation thứ hai cho cùng route;
- không redesign khi chuyển sang Vue;
- visible UI dùng tiếng Việt đơn giản;
- module frontend theo product capability, không theo mọi backend class;
- route/page state phải có Loading / Empty / Error khi phù hợp;
- mỗi frontend module có `bugs.md` và `fixed.md`.

## Sidebar groups

```text
TỔNG QUAN
HỌC TẬP
NỀN TẢNG
ÔN TẬP & KIỂM TRA
TIẾN ĐỘ
MỞ RỘNG
TÀI KHOẢN
```

Chi tiết đọc `docs/product/00-navigation-and-learning-flow.md`.
