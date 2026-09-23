# Authentication

Scope:
- đăng ký/đăng nhập theo provider được chốt sau;
- session/token lifecycle;
- logout;
- recovery/reset nếu provider hỗ trợ và scope cần.

Không tự chọn provider trong implementation nếu chưa có decision record.

LearnerProfile tách khỏi credential/provider-specific identity.

## Runtime boundary hiện tại

Backend có adapter JWT/OIDC provider-agnostic, chỉ bật khi cấu hình đủ:

```text
Authentication__Jwt__Enabled=true
Authentication__Jwt__Authority=<issuer của provider đã được chọn>
Authentication__Jwt__Audience=<audience của API>
```

Khi bật, ASP.NET Core JwtBearer validate token qua authority và `IUserContextAccessor` lấy
`sub`/`NameIdentifier` cùng các claim `permission` hoặc `permissions`. Ứng dụng không tự chọn
provider, không lưu password và không coi role display name là permission.

Khi tắt (mặc định local), chỉ Development mới cho phép `X-Dev-User-Id` và `X-Dev-Permission`;
header này không được dùng để xác thực production.

## Worker callback authentication

Callback từ worker không dùng learner JWT hoặc dev permission. Khi bật production callback,
cấu hình:

```text
Workers__Callback__SharedSecret=<secret dùng chung qua secret manager>
Workers__Callback__MaxClockSkewSeconds=300
```

Worker ký payload nguyên bản bằng HMAC-SHA256 trên chuỗi:

```text
<unix timestamp>\n<raw JSON body>
```

và gửi `X-VietAIS-Worker-Timestamp` cùng
`X-VietAIS-Worker-Signature: v1=<base64url signature>`. API từ chối timestamp quá cũ, chữ ký
sai hoặc callback thiếu `UserId`. `JobId` vẫn là correlation/idempotency key của Exam; secret
không được ghi vào log hay commit vào repository.
