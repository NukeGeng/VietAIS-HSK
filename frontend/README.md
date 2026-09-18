# VietAIS HSK frontend

Frontend production đầu tiên của VietAIS HSK, dựng theo `docs/frontend` và template đã duyệt.

## Chạy local

```bash
npm install
npm run dev
```

Vite proxy `/api` tới `http://127.0.0.1:5055`. Chỉ khi kiểm tra local mới có thể đặt `VITE_DEV_USER_ID` để gửi dev header; production phải dùng claims từ provider xác thực.

## Kiểm tra

```bash
npm run build
```
