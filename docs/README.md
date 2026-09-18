# Documentation Index

```text
docs/
├── product/
│   ├── 00-navigation-and-learning-flow.md
│   └── 01-feature-scope.md
├── architecture/
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

`design-template/` là source of truth trực quan của UI; `docs/` là source of truth nghiệp vụ/kỹ thuật.
