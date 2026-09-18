# Fixed

## REVIEW-FIX-001 — Practice signal to deterministic review queue

### Thay đổi

- Thêm `PracticeEvaluationSignal` contract nội bộ giữa Practice và Review.
- Câu sai tạo/merge một `ReviewItem` theo composite identity, tăng mistake count và đưa item về due ngay.
- Câu đúng giảm priority; review result đúng có thể resolve item và schedule lần kế tiếp.
- Thêm summary, items, needs-review, mistakes, review session và result endpoints.
- Giữ scheduler bootstrap deterministic; algorithm cuối cùng vẫn là open decision.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm Practice wrong → review item → review result → resolve.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Review không sở hữu master knowledge data.
- User isolation được kiểm tra qua context hiện tại.
- Không dùng AI/RabbitMQ cho core review bootstrap.
