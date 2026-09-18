# WORKFLOW.md

## Chu trình chuẩn

```text
Docs
→ Design template (nếu có UI)
→ Implement
→ Automated Test
→ Run
→ Computer Use Test
→ bugs.md
→ Fix
→ Retest
→ fixed.md
→ Module Done
```

## Trước task

1. Đọc `GOAL.md`.
2. Đọc `docs/product/` liên quan.
3. Đọc architecture/data/security/building-block cần thiết.
4. Đọc module `.md`, `bugs.md`, `fixed.md`.
5. Xác nhận scope/dependency.

## Không làm

- không code khi module docs chưa đủ để hiểu scope;
- không sửa ngoài scope;
- không merge khi còn Critical/High bug;
- không xóa bug history;
- không ghi fixed nếu chưa retest;
- không redesign approved UI khi chuyển HTML → Vue;
- không over-engineering;
- không biến sidebar item thành backend module nếu boundary hiện tại đã đủ.

## Design-template

Hiện tại UI được duyệt bằng HTML/CSS trong `design-template/`.

Production frontend chỉ implement sau khi page tương ứng được duyệt.

## Branch flow gợi ý

```text
main
  ↓
develop
  ↓
module/<domain>-<name>
  ↓
feature/<scope>
```

Hoàn thành module:

```text
Draft PR
→ review diff
→ automated tests
→ Computer Use tests
→ update bugs/fixed docs
→ submit PR
→ merge develop
→ cleanup branch
```
