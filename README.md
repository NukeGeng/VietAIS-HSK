# VietAIS-HSK

Nền tảng học tiếng Trung theo lộ trình HSK 3.0.

## Cấu trúc hiện tại

- `docs/` — tài liệu chính để xây dựng hệ thống VietAIS HSK;
- `frontend/` — frontend Vue 3/Vite production đầu tiên;
- `src/VietAisHsk.Api/` — API Modular Monolith ASP.NET Core;
- `scripts/` — smoke test HTTP tích hợp;
- `design-template/` — template HTML/CSS tham chiếu, không phải nơi chứa system docs.

## Kiểm tra nhanh

```bash
dotnet build VietAisHsk.slnx --no-restore
cd frontend && npm run build
```

Chạy API ở `http://127.0.0.1:5055` và frontend Vite ở `http://127.0.0.1:5173` để kiểm tra tích hợp local.
