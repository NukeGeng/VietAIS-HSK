# Authentication

Scope:
- đăng ký/đăng nhập theo provider được chốt sau;
- session/token lifecycle;
- logout;
- recovery/reset nếu provider hỗ trợ và scope cần.

Không tự chọn provider trong implementation nếu chưa có decision record.

LearnerProfile tách khỏi credential/provider-specific identity.
