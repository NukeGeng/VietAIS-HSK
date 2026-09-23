# Fixed

## TRANSLATION-FIX-001 — Persist attempt before optional feedback

### Thay đổi

- Thêm exercise catalog contract, learner-owned translation attempt và history query.
- Attempt được lưu độc lập trước khi feedback được yêu cầu.
- Feedback chỉ được gọi qua `ITranslationFeedbackGateway` sau request explicit.
- Provider chưa được quyết định nên gateway trả 503 có cấu trúc; attempt vẫn còn nguyên.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm submit không gọi AI và provider failure không làm mất attempt.

### Computer Use

- Chưa áp dụng; production frontend chưa triển khai.

### Kết quả

- Không tự gọi AI khi learner chỉ submit attempt.
- Không khóa provider/model trong implementation.
- Feedback schema sẵn sàng để nối AI Gateway sau khi decision được chốt.

## TRANSLATION-FIX-002 — Public translation views không lộ reference Chinese

- Tách exercise/attempt view khỏi domain model.
- Learner response không còn trả `ReferenceChinese`; gateway vẫn nhận domain attempt nội bộ.
- Smoke test assert catalog và attempt không có reference answer.

## TRANSLATION-FIX-003 — Translation attempt phát Progress signal

### Thay đổi

- Khi learner submit attempt, endpoint phát `TranslationAttemptSignal` với event id ổn định theo attempt.
- Progress ghi activity `translation-attempt` để tính history/streak và đếm `TranslationAttempts` trong snapshot.
- In-memory và Marten Progress đều dedupe signal, giữ signal khi rebuild từ các input khác.
- Không chấm đúng/sai và không gọi AI trong signal này; feedback vẫn chỉ chạy sau request explicit.

### Kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 50/50.
- `scripts/identity-smoke.mjs` kiểm tra translation attempt và Progress signal trong cùng learner flow.
- HTTP QA 2026-09-22: `translationAttempts=1`, activity `translation-attempt` xuất hiện, không có duplicate sau retry flow.

## TRANSLATION-FIX-004 — Feedback contract, timeout và scope an toàn

### Thay đổi

- `TranslationValidation` kiểm tra đủ sáu field của structured feedback và giới hạn kích thước field trước khi lưu.
- Feedback gateway chạy với timeout 5 giây; timeout trả `504`, provider exception trả `502`, provider chưa cấu hình vẫn trả `503`; mọi trường hợp đều giữ attempt đã lưu.
- Feedback/history lookup tiếp tục truyền `userId` vào store, nên attempt của learner khác trả `404` thay vì lộ dữ liệu.
- Frontend phân biệt timeout/provider failure để hiển thị lỗi an toàn, không mất câu trả lời.

### Kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests --no-restore -m:1` — validation contract pass.
- `scripts/identity-smoke.mjs` kiểm tra learner khác không thể request feedback trên attempt của user hiện tại; provider chưa cấu hình trả `503` và history vẫn giữ attempt.
- Computer Use QA 2026-09-22: mở `/app/skills/translation`, nhập `我喜欢学习中文。`, lưu attempt thành công, nút `Nhận góp ý` xuất hiện; provider chưa cấu hình hiển thị lỗi an toàn nhưng attempt vẫn còn trong `Lịch sử gần đây`. Screenshot browser không có clipping — pass.
