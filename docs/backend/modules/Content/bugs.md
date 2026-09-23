# Bugs

## CONTENT-BUG-001 — Question bank từng nằm trong Practice module

- Ngày: 2026-09-22
- Mức độ: Medium
- Flow: Practice đọc question definition để tạo session.
- Actual: bootstrap question catalog được đặt trực tiếp trong `PracticeStore.cs`, làm mờ boundary
  Content sở hữu question bank.
- Trạng thái: Fixed — xem `CONTENT-FIX-001`.
