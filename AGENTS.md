# AGENTS.md

## First rule

Read `GOAL.md` first.

Then read only documents required by the current task.

## Documentation-first

For any module task:

1. Read relevant `docs/product/` scope.
2. Read the module `<Module>.md`.
3. Read `bugs.md`.
4. Read `fixed.md`.
5. Read relevant architecture/data/security/building-block docs.
6. Confirm scope and dependencies.
7. Implement.
8. Run automated tests.
9. Use Computer Use for real UI flow when applicable.
10. Record bugs.
11. Fix, retest and update `fixed.md`.

## Current UI rule

`frontend/` is the only production UI runtime. `design-template/` HTML/CSS is the approved panda
visual reference/QA fixture before Vue implementation; it is not a second frontend and must not be
imported into the production bundle.

Khi làm bất kỳ task nào liên quan:

```text
HTML design-template
Frontend
Vue
Admin UI
Learner UI
Responsive
Layout

docs/frontend/architecture/00-overview.md
docs/frontend/architecture/01-ui-layout-rules.md

PageHeader
PageHero
BlockingHeader
ContentHeader lớn
breadcrumb không cần thiết
mô tả marketing đầu trang
```

Hai tài liệu layout bắt buộc là:

```text
docs/frontend/architecture/00-overview.md
docs/frontend/architecture/01-ui-layout-rules.md
```

Mặc định phải theo `Content first. No blocking PageHeader by default.`: giữ tiêu đề ngắn,
đưa filter/action gần nội dung và không thêm mô tả marketing, breadcrumb hoặc khoảng trắng lớn
nếu sidebar đã cung cấp context.

## No architecture invention

Do not introduce:
- new architectural style;
- new business module;
- new messaging pattern;
- new shared abstraction;
- new cross-module contract;
- new dependency

unless documented need exists and scope is updated/approved.

A new sidebar item is NOT automatically a new backend module.

## Tests

Do not delete tests to make builds pass.

Build success != done.

## Permissions

Never hard-code permission behavior by display role name.

## Docs are part of implementation

If behavior changes, update the corresponding Markdown in the same task.

Task is incomplete if docs and implementation disagree.
