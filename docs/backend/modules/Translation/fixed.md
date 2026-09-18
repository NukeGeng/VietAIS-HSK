# Fixed

## TRANSLATION-FIX-001 — Persist attempt before optional feedback

### Thay đổi

- Thêm exercise catalog contract, learner-owned translation attempt và history query.
- Attempt được lưu độc lập trước khi feedback được yêu cầu.
- Feedback chỉ được gọi qua `ITranslationFeedbackGateway` sau request explicit.
- Provider chưa được quyết định nên gateway trả 503 có cấu trúc; attempt vẫn còn nguyên.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm submit không gọi AI và provider failure không làm mất attempt.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Không tự gọi AI khi learner chỉ submit attempt.
- Không khóa provider/model trong implementation.
- Feedback schema sẵn sàng để nối AI Gateway sau khi decision được chốt.
