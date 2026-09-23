# Documentation Index

```text
docs/
├── product/
│   ├── 00-navigation-and-learning-flow.md
│   └── 01-feature-scope.md
├── architecture/
│   └── 08-repository-layout.md
├── data/
├── security/
├── backend/
│   ├── api/
│   ├── building-blocks/
│   ├── modules/
│   ├── workers/
│   └── testing/
├── frontend/
│   ├── architecture/
│   └── modules/
├── deploy/
├── decisions/
└── templates/
```

## Nguyên tắc tài liệu

Mỗi business module có thư mục riêng:

```text
<Module>/
├── <Module>.md
├── bugs.md
└── fixed.md
```

Không gộp toàn bộ module vào một file lớn.

## Thứ tự đọc khi triển khai module

```text
Product scope
→ Architecture
→ Data/Security liên quan
→ API/Building Blocks
→ Module doc
→ Implement/Test
```

`design-template/` là reference/QA fixture trực quan của UI; `frontend/` là runtime UI production duy
nhất; `docs/` là source of truth nghiệp vụ/kỹ thuật. Template được chạy độc lập để đối chiếu visual,
không được deploy như frontend thứ hai và không được import vào production bundle.

Ranh giới thư mục được kiểm tra bằng `node scripts/verify-repository-layout.mjs`.
