# GITHUB_RULES.md

# GitHub Rules — HSK Learning Platform

## 1. Branch flow

```text
main
  ↓
develop
  ↓
module/<scope>-<name>
  ↓
feature/<scope>-<name>
```

Ví dụ:

```text
main
└── develop
    ├── module/be-exam
    │   ├── feature/be-start-exam
    │   ├── feature/be-submit-answer
    │   └── feature/be-exam-result
    ├── module/fe-learning
    ├── module/data-hsk
    └── module/deploy-infra
```

## 2. Branch roles

### `main`
Chỉ chứa phiên bản ổn định.

- Không commit trực tiếp.
- Chỉ merge từ `develop`.
- Chỉ merge khi build/test/staging/critical flow pass.
- Không còn bug Critical/High chưa xử lý.

### `develop`
Branch tích hợp chính.

- Không commit feature trực tiếp.
- Nhận PR từ `module/*`, `docs/*`, `hotfix/*` khi phù hợp.

### `module/*`
Tạo từ `develop`.

Format:

```text
module/<scope>-<module>
```

Scope:

```text
be
fe
data
deploy
docs
design
```

Ví dụ:

```text
module/be-exam
module/be-curriculum
module/fe-learning
module/fe-speaking
module/data-hsk3
module/design-app-shell
module/deploy-staging
```

Flow:

```text
Draft PR
→ review diff
→ build/test
→ Computer Use test nếu có UI
→ bugs.md/fixed.md
→ Ready for review
→ merge develop
```

### `feature/*`
Tạo từ branch module tương ứng.

Format:

```text
feature/<scope>-<short-name>
```

Ví dụ:

```text
feature/be-submit-exam
feature/be-exam-projection
feature/fe-hanzi-writing
feature/fe-review-page
feature/data-pinyin-import
```

Flow:

```text
feature/*
→ module/*
```

Không merge trực tiếp feature vào `develop` trừ khi có lý do đặc biệt được review.

## 3. Commit rules

Format khuyến nghị:

```text
<type>(<scope>): <message>
```

Type:

```text
feat
fix
refactor
test
docs
chore
style
build
ci
```

Ví dụ:

```text
feat(exam): add exam submission flow
fix(hanzi): correct stroke validation state
docs(curriculum): update beginner learning flow
test(review): add review queue integration tests
```

Tránh commit message:

```text
update
fix
done
test
abc
final
final2
```

Không commit quá vụn.

## 4. Pull Request flow

### Feature PR

```text
feature/*
→ module/*
```

Yêu cầu:

- scope nhỏ;
- không sửa ngoài module;
- build pass;
- test liên quan pass.

### Module PR

```text
module/*
→ develop
```

Bắt buộc mở Draft PR trước.

Checklist:

```text
1. Preview diff
2. Review scope
3. Build
4. Unit test
5. Integration test
6. Permission test nếu có
7. Computer Use test nếu có UI
8. Update bugs.md
9. Update fixed.md
10. Update module docs
11. Ready for review
12. Merge
```

## 5. Documentation rules

Project dùng documentation-driven development.

Trước khi implement module:

```text
GOAL.md
→ relevant architecture docs
→ relevant module docs
→ bugs.md
→ fixed.md
```

Nếu behavior thay đổi thì docs phải thay đổi trong cùng PR.

Không chấp nhận:

```text
code mới
nhưng docs cũ
```

## 6. Bug rules

Khi test phát hiện lỗi, ghi vào:

```text
docs/.../<Module>/bugs.md
```

Phải có:

- ID;
- ngày;
- mức độ;
- flow tái hiện;
- expected;
- actual;
- trạng thái.

Không xóa bug sau khi fix.

Sau khi fix, ghi checklist vào:

```text
docs/.../<Module>/fixed.md
```

## 7. Computer Use rules

Module có UI bắt buộc kiểm thử bằng browser/Computer Use trước khi merge vào `develop`.

Test tối thiểu:

```text
render
navigation
loading
empty
error
permission
responsive
critical user flow
```

Nếu lỗi:

```text
bugs.md
→ fix
→ retest
→ fixed.md
```

## 8. Backend PR requirements

```text
[ ] build pass
[ ] unit tests pass
[ ] integration tests pass
[ ] permissions đúng
[ ] Marten transaction đúng
[ ] projection đúng
[ ] message idempotency nếu có
[ ] RabbitMQ chỉ dùng khi cần
[ ] không event-source không cần thiết
[ ] không thêm dependency thừa
[ ] docs cập nhật
```

## 9. Frontend PR requirements

```text
[ ] bám approved design-template
[ ] không tự redesign
[ ] desktop pass
[ ] mobile pass
[ ] loading state
[ ] empty state
[ ] error state
[ ] permission state
[ ] không mất dấu tiếng Việt
[ ] chữ Hán đúng
[ ] Pinyin đúng
[ ] không clipping / overflow
[ ] Computer Use flow pass
```

## 10. Design-template rules

Trong phase thiết kế HTML:

```text
design-template/
```

là source of truth trực quan.

Không:

- thêm Vue;
- thêm backend;
- gọi API thật;
- đưa business logic production vào template.

Chỉ dùng:

```text
HTML
CSS
JavaScript nhẹ
mock data
```

## 11. Merge rules

Ưu tiên:

```text
Squash merge
```

cho:

```text
feature → module
module → develop
```

Không merge khi:

```text
Critical bug OPEN
High bug OPEN
required tests fail
docs chưa cập nhật
Computer Use flow fail
```

## 12. Branch cleanup

Sau merge:

```text
delete merged feature branch
delete merged module branch
```

Dọn branch:

- đã merge;
- bỏ lâu;
- sai kiến trúc;
- không còn dùng.

## 13. GitHub branch protection / rulesets

### `main`

Bắt buộc:

```text
Require pull request
Require at least 1 approval
Require status checks
Require branch up to date
Block force push
Block deletion
No direct push
```

Recommended checks:

```text
build
unit-tests
integration-tests
architecture-tests
```

### `develop`

Bắt buộc:

```text
Require pull request
Require status checks
Block force push
Block deletion
```

Có thể yêu cầu 1 approval.

## 14. Suggested CI checks

PR vào `develop` hoặc `main` nên chạy:

```text
Backend:
dotnet restore
dotnet build
dotnet test

Frontend:
npm ci
npm run build
npm run test

Docs:
markdown check nếu có

Optional:
architecture tests
lint
format check
```

## 15. PR title

Format:

```text
<type>(<scope>): <summary>
```

Ví dụ:

```text
feat(exam): implement exam attempt flow
fix(hanzi): fix writing canvas validation
docs(goal): update module implementation workflow
```

## 16. PR description template

```md
## Scope

Mô tả ngắn thay đổi.

## Docs read

- [ ] GOAL.md
- [ ] Module docs
- [ ] bugs.md
- [ ] fixed.md
- [ ] Relevant architecture docs

## Changes

- ...

## Tests

- [ ] Build
- [ ] Unit
- [ ] Integration
- [ ] Permission
- [ ] Computer Use

## Bugs

- Bugs found:
- Bugs fixed:

## Docs updated

- [ ] Module docs
- [ ] bugs.md
- [ ] fixed.md

## Out of scope

- ...
```

## 17. Core rule

```text
Docs
→ Code
→ Test
→ Computer Use
→ Bugs
→ Fix
→ Retest
→ fixed.md
→ PR
→ Merge
```

Build success alone does not mean the task is complete.
