# Frontend Overview

Production frontend chỉ bắt đầu sau khi page tương ứng đã được duyệt trong `design-template/`.

Stack:

- Vue 3;
- TypeScript;
- Vite;
- Vue Router;
- Pinia.

## Nguyên tắc

- approved HTML/CSS là visual source of truth;
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
