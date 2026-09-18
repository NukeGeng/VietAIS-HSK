# Authorization

## Actors

- Guest;
- Learner;
- Admin.

Role chỉ là cách cấp permission; business handler không hard-code logic theo display role name.

Permission gợi ý theo capability:

```text
curriculum.read
curriculum.manage
practice.use
exam.take
content.manage
users.manage
ai.usage.view
```

Tên permission cuối cùng phải thống nhất trong Identity/Security implementation.

## Learner

Chỉ được:
- đọc published curriculum/content;
- thao tác dữ liệu học của chính mình;
- xem progress của chính mình.

## Admin

Chỉ có quyền quản trị tương ứng permission được cấp.
