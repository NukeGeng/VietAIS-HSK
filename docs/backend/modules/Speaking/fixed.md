# Fixed

## SPEAKING-FIX-001 — Session/turn contract with provider fallback

### Thay đổi

- Thêm speaking session theo HSK context/mode, own-session access và history.
- Persist learner turn trước khi gọi provider seam.
- Khi provider chưa cấu hình, turn vẫn được lưu với `providerStatus=unavailable` để UI hiển thị retry state.
- End session idempotent.

### Automated test

- `dotnet build backend/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm start/turn/provider fallback/end/history.

### Computer Use

- Chưa áp dụng; microphone/realtime transport chưa được chốt hoặc triển khai.

### Kết quả

- Chưa chọn STT/TTS/DeepSeek provider.
- Không dùng RabbitMQ cho per-turn.
- Session context giữ HSK level để level guard có thể nối vào provider sau này.

## SPEAKING-FIX-002 — Completed session phát Progress signal

### Thay đổi

- Khi kết thúc speaking session, endpoint phát `SpeakingSessionCompletedSignal` với context, số lượt nói và thời điểm hoàn tất.
- Progress ghi activity `speaking-session-completed`, tính streak và đếm `SpeakingSessions/SpeakingTurns`.
- Gọi lại endpoint end sau khi session đã kết thúc vẫn trả session cũ nhưng signal/activity không bị cộng trùng.
- Không thêm chấm điểm phát âm giả khi STT/provider chưa được cấu hình.

### Kiểm chứng

- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 50/50.
- `scripts/identity-smoke.mjs` kiểm tra start/turn/end và end retry.
- HTTP QA 2026-09-22: `speakingSessions=1`, `speakingTurns=1`, activity `speaking-session-completed` xuất hiện sau hai lần end.

## SPEAKING-FIX-003 — Provider failure an toàn và session scope

### Thay đổi

- Provider call có timeout 5 giây; timeout và exception được chuyển thành `providerStatus=timeout|error`, vẫn lưu learner turn và trả `retry-provider` thay vì làm request thành 500.
- Provider chưa cấu hình giữ trạng thái `unavailable`; frontend hiển thị thông báo tương ứng cho cả ba trạng thái không thành công.
- Mọi read/write/end session đều lookup theo `userId`; learner khác chỉ nhận `404` và không thể chạm vào session.

### Kiểm chứng

- `scripts/identity-smoke.mjs` kiểm tra learner khác không đọc/ghi được session; provider fallback vẫn lưu turn và end idempotent.
- `dotnet test tests/VietAisHsk.Api.Tests --no-restore -m:1` — pass.
- STT transport và TTS provider vẫn để pending vì contract realtime/audio chưa được chốt trong docs.
