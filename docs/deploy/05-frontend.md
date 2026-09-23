# Frontend Deployment

Frontend được build bằng `deploy/Dockerfile.frontend` rồi phục vụ static qua Nginx. Nginx giữ
history fallback về `index.html` cho Vue Router và proxy `/api/` tới service `api`, nên production
build không hard-code host backend.

Local endpoint: `http://127.0.0.1:5173`. Nginx expose `/health` bằng cách proxy tới API liveness
endpoint để compose có thể kiểm tra cả frontend gateway. Cache headers/CDN và asset versioning
cần được cấu hình ở lớp hosting production.
