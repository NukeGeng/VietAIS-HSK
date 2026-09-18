# Identity Module

## 1. Mục đích

Quản lý danh tính ứng dụng, hồ sơ learner, thiết lập học cơ bản và authorization context.

Credential/provider cụ thể là decision riêng; module không phụ thuộc UI login provider cụ thể trong business logic.

## 2. Phạm vi

### Có làm
- liên kết user application với auth identity;
- LearnerProfile;
- tên hiển thị/avatar metadata nếu có;
- HSK mục tiêu / mức HSK ưu tiên hiển thị;
- múi giờ;
- thiết lập học cơ bản;
- trạng thái tài khoản;
- permission assignment/read model cho authorization.

### Không làm
- tiến độ bài học;
- streak;
- điểm yếu;
- password storage tự chế nếu auth provider xử lý;
- school/organization tenancy.

## 3. Actor & phân quyền

| Actor | Quyền |
|---|---|
| Guest | register/login flow theo auth provider; đọc public auth metadata cần thiết |
| Learner | xem/sửa hồ sơ của chính mình |
| Admin | xem user và thay đổi trạng thái/quyền khi có `users.manage` |

Không hard-code business rule theo tên role hiển thị.

## 4. Chức năng

### 4.1 Hồ sơ learner

Flow:

```text
Login thành công
→ resolve/create application user
→ load LearnerProfile
→ user cập nhật profile/preferences
```

Profile tối thiểu:
- DisplayName;
- PreferredHskLevelId (chỉ preference, Learning mới sở hữu progress);
- TargetHskLevelId;
- Timezone;
- StudyPreferences.

### 4.2 HSK mục tiêu

Mục đích: giúp UI/lộ trình mặc định phù hợp.

Không dùng field này làm bằng chứng learner đã hoàn thành level.

### 4.3 Authorization context

Request handler kiểm tra policy/permission.

Không kiểm tra kiểu:

```text
if roleName == "Admin"
```

trong business use case.

## 5. Invariants

- UserId application là stable identifier nội bộ.
- Learner chỉ sửa profile của mình.
- Preferred/Target HSK phải reference level tồn tại khi validation có thể thực hiện.
- Timezone phải là timezone identifier hợp lệ theo chuẩn app chọn.

## 6. Data model

### Documents

```text
UserAccount
LearnerProfile
PermissionAssignment / AuthorizationReadModel (tùy auth implementation)
```

Không Event Source Identity ở MVP.

## 7. Commands

| Command | Mục đích |
|---|---|
| EnsureUserProvisioned | tạo application user sau auth lần đầu |
| UpdateLearnerProfile | cập nhật hồ sơ |
| SetLearningTarget | cập nhật HSK mục tiêu/preference |
| UpdateStudyPreferences | thiết lập học |
| ChangeUserStatus | admin khóa/mở trạng thái khi được phép |

## 8. Queries

| Query | Mục đích |
|---|---|
| GetMyProfile | hồ sơ learner hiện tại |
| GetMyAuthorizationContext | permission/context |
| GetUserForAdmin | admin xem user |
| SearchUsers | admin list/search |

## 9. Events / messages

Không cần Event Sourcing.

Chỉ phát application message khi module khác thực sự cần biết thay đổi, ví dụ `LearnerProfileUpdated`; không phát event cho mọi field change nếu không có consumer.

## 10. API gợi ý

| Method | Route | Permission | Mục đích |
|---|---|---|---|
| GET | `/api/me` | authenticated | profile hiện tại |
| PUT | `/api/me/profile` | authenticated | sửa profile |
| PUT | `/api/me/learning-target` | authenticated | HSK mục tiêu/preference |
| GET | `/api/admin/users` | `users.manage` | list user |
| GET | `/api/admin/users/{id}` | `users.manage` | chi tiết user |
| PATCH | `/api/admin/users/{id}/status` | `users.manage` | trạng thái user |

Route cuối cùng phải theo API convention chung.

## 11. RabbitMQ

Không.

## 12. AI

Không.

## 13. Dependencies

- Security docs;
- Curriculum chỉ để validate HSK reference khi cần.

Không để Identity phụ thuộc Learning progress.

## 14. Test cases

### Unit
- [ ] learner không sửa profile user khác;
- [ ] target/preference validation;
- [ ] permission checks.

### Integration
- [ ] first-login provisioning idempotent;
- [ ] profile persistence;
- [ ] admin user query.

### Computer Use
- [ ] login → mở Hồ sơ → sửa → reload vẫn đúng;
- [ ] learner không truy cập admin users;
- [ ] admin có permission truy cập được.

## 15. Acceptance Criteria

- [ ] auth identity map đúng application user;
- [ ] profile CRUD đúng scope;
- [ ] permission không hard-code role name;
- [ ] không có progress data bị sở hữu sai trong Identity.
