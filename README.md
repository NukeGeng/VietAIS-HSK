# VietAIS-HSK

Nền tảng học tiếng Trung theo lộ trình HSK 3.0.

## Cấu trúc hiện tại

- `docs/` — tài liệu chính để xây dựng hệ thống VietAIS HSK;
- `frontend/` — frontend Vue 3/Vite production đầu tiên;
- `backend/VietAisHsk.Api/` — API Modular Monolith ASP.NET Core;
- `backend/VietAisHsk.Workers/` — process worker riêng cho các job async, không nằm trong API process;
- `scripts/` — smoke test HTTP tích hợp;
- `design-template/` — checkout mẫu HTML/CSS panda đã duyệt để đối chiếu khi làm UI trong `frontend/`; không phải production frontend và không được import vào bundle.

## Kiểm tra nhanh

```bash
dotnet build VietAisHsk.slnx --no-restore
cd frontend && npm run build
```

Chạy API bằng `dotnet run --project backend/VietAisHsk.Api` ở `http://127.0.0.1:5055` và frontend Vite ở `http://127.0.0.1:5173` để kiểm tra tích hợp local.

Preview HTML/CSS trong `design-template/` chạy độc lập trên cổng `8766`:

```bash
./design-template/scripts/serve-preview.sh
```

Mở `http://localhost:8766/index.html`. Nếu tab browser cũ từng báo
`ERR_CONNECTION_REFUSED`, hãy refresh hoặc mở lại URL sau khi server đã chạy; kiểm tra
nhanh bằng:

```bash
curl -fsS -o /dev/null -w '%{http_code}\n' http://localhost:8766/index.html
```

Kết quả đúng là `200`. Preview này chỉ phục vụ `design-template/`, không thay thế
frontend Vue production.

Kiểm tra ranh giới repository trước khi build:

```bash
node scripts/verify-repository-layout.mjs
```

Lưu ý route: các đường dẫn dạng `/app/skills/listening` là route của frontend
Vue trên `http://127.0.0.1:5173`. Preview HTML dùng các file tĩnh tương ứng,
ví dụ `http://localhost:8766/app/listening.html` hoặc
`http://localhost:8766/app/index.html#skills`; không ghép route Vue vào server
HTML tĩnh.

## Local deployment contract

Để chạy stack gần production gồm PostgreSQL, RabbitMQ, API và frontend static qua Nginx:

```bash
docker compose up --build
```

Khi cần chạy thêm worker async, bật profile `workers`:

```bash
docker compose --profile workers up --build
```

Mở `http://127.0.0.1:5173`. API health là `http://127.0.0.1:5055/health`;
RabbitMQ management là `http://127.0.0.1:15672` với tài khoản local trong `compose.yaml`.

Compose bật Wolverine chỉ trong stack này khi cả Postgres và RabbitMQ healthy. Worker AI thật,
object storage và credentials production không được giả lập trong local compose. JWT/OIDC production
được bật bằng `Authentication__Jwt__*` sau khi provider được chốt; local mặc định dùng dev adapter.
