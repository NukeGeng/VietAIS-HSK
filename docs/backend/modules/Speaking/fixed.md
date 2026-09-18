# Fixed

## SPEAKING-FIX-001 — Session/turn contract with provider fallback

### Thay đổi

- Thêm speaking session theo HSK context/mode, own-session access và history.
- Persist learner turn trước khi gọi provider seam.
- Khi provider chưa cấu hình, turn vẫn được lưu với `providerStatus=unavailable` để UI hiển thị retry state.
- End session idempotent.

### Automated test

- `dotnet build src/VietAisHsk.Api/VietAisHsk.Api.csproj --no-restore` — pass.
- `node scripts/identity-smoke.mjs` — pass, gồm start/turn/provider fallback/end/history.

### Computer Use

- Chưa áp dụng; microphone/realtime transport chưa được chốt hoặc triển khai.

### Kết quả

- Chưa chọn STT/TTS/DeepSeek provider.
- Không dùng RabbitMQ cho per-turn.
- Session context giữ HSK level để level guard có thể nối vào provider sau này.
