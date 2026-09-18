# Fixed

## PROGRESS-FIX-001 — Rule-based practice projection bootstrap

### Thay đổi

- Thêm progress snapshot, weak points, meaningful history và streak read endpoints.
- Practice answer phát signal vào Progress; câu sai tạo weakness có evidence/action.
- Practice completion ghi history theo activity key idempotent, không tính click/page view.
- Streak dùng qualifying activity và timezone fallback UTC ở bootstrap.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm practice projection, weak point và duplicate completion idempotency.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Progress không sở hữu attempt/source data.
- Không dùng AI.
- Projection hiện là in-memory bootstrap; replay/rebuild và Marten async projection vẫn là việc cần làm trước staging.
