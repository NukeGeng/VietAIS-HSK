# Bugs

## GRAMMAR-FE-BUG-001 — Route Ngữ pháp từng dùng placeholder chung

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: mở `/app/grammar`, lọc HSK, mở detail và lesson liên quan.
- Actual: route chưa có list/detail UI gắn với Curriculum read contract.
- Trạng thái: Fixed — xem `GRAMMAR-FE-FIX-001`.

Không còn bug mở đã biết. Mobile visual regression vẫn pending.

## GRAMMAR-FE-BUG-002 — Đổi list → detail không đồng bộ route param

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: mở `/app/grammar`, bấm một cấu trúc.
- Actual: URL đổi nhưng list vẫn render do component không theo dõi `route.params.id`.
- Trạng thái: Fixed — xem `GRAMMAR-FE-FIX-002`.
