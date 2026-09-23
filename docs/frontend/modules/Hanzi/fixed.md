# Fixed

## HANZI-FE-FIX-001 — Nối catalog Chữ Hán vào learner UI

### Thay đổi

- Thay page placeholder bằng route-aware `HanziView` cho danh sách, chi tiết, thứ tự nét và luyện viết.
- Danh sách có tìm kiếm; detail hiển thị chữ Hán, Pinyin, nghĩa, HSK, bộ thủ và số nét.
- Stroke view render reference path có replay/hiện toàn bộ nét; writing view dùng canvas pointer, mode selector và gửi từng stroke tới Practice backend.
- UI hiển thị feedback deterministic, cho retry, hoàn tất attempt và không hiển thị điểm số giả.

### Automated test

- `npm run build` — pass.
- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore` — pass, 29/29.

### Computer Use

- Desktop list/detail/stroke/writing — đã QA sau khi API catalog được rebuild.
- Desktop writing: sai nét → feedback → retry → đủ nét → hoàn tất — đã QA bằng API dev user.
- Computer Use 2026-09-22: tìm `lớn` → mở `大` → chạy animation `0/3 → 3/3` → cố ý sai nét nhận feedback → retry → viết đủ `3/3` → hoàn tất với trạng thái `Hoàn thành đúng các nét.` — pass.
- Computer Use 2026-09-22: mở route luyện viết, refresh trình duyệt và vẫn giữ attempt hiện tại qua `sessionStorage` + `GET /api/practice/hanzi/attempts/{id}`; mode của attempt được khôi phục đúng — pass.
- Responsive CSS đã được thêm cho list/detail/stroke/writing; mobile viewport cần được chạy trong vòng QA responsive chuyên biệt trước khi đánh dấu hoàn tất.

## HANZI-FE-FIX-003 — Giữ attempt luyện viết khi refresh

### Thay đổi

- Lưu `HanziWritingAttempt.id` theo từng chữ Hán trong `sessionStorage`, không tạo attempt mới mỗi lần component mount.
- Khi mở lại route, đọc attempt đã lưu qua API để vẫn tôn trọng ownership và trạng thái backend; nếu attempt không còn tồn tại thì xóa key cũ và tạo attempt mới.
- Khôi phục cả mode `Guided` / `Trace` / `Recall`; các nét đã gửi vẫn do backend giữ, còn canvas chỉ vẽ lại nét mới trong phiên hiện tại vì public attempt view không phát hành raw pointer data.

### Automated test

- `npm run build` — pass.
- `dotnet test tests/VietAisHsk.Api.Tests/VietAisHsk.Api.Tests.csproj --no-restore -m:1` — pass, 60/60.

### Giới hạn còn lại

- Catalog hiện là `PlatformAuthoredReferenceFixture` tối thiểu, chưa phải bộ dữ liệu HSK 3.0 chính thức.
- Reference catalog và tolerance hiện vẫn là fixture PoC.

## HANZI-FE-FIX-004 — QA canvas luyện viết trên mobile

### Thay đổi / xác nhận

- Giữ canvas theo chiều rộng khả dụng (`width: min(100%, 360px)` và `aspect-ratio: 1`) để không tạo overflow ngang ở viewport hẹp.
- Các nút hoàn tất, làm lại và xem nét mẫu vẫn nằm trong card và có thể cuộn tới được; không cần thu nhỏ chữ hoặc ẩn nội dung để vừa viewport.
- Pointer input trên canvas hoạt động ở mobile: một nét hợp lệ được ghi nhận từ `0 / 3 nét` thành `1 / 3 nét` và trả feedback deterministic.

### Computer Use visual QA — 2026-09-22

- Production Vite: `http://127.0.0.1:5173/app/hanzi/hanzi-da/write?qa=hanzi-mobile-20260922`.
- Viewport: `390×844`.
- Đã kiểm tra screenshot ở đầu và cuối vùng cuộn; canvas không clipping, không overflow ngang, action row và provenance text đều truy cập được.
- Đã drag một nét trên canvas; AX state hiển thị `1 / 3 nét` và `Nét đúng. Tiếp tục nét tiếp theo.` — pass.

## HANZI-FE-FIX-005 — Nối route Từ liên quan vào Hanzi catalog

### Thay đổi / xác nhận

- `/app/hanzi/related` không còn dùng `PageView` placeholder; router render `HanziView` ở mode
  `related` và dùng cùng `GET /api/hanzi` read contract như danh sách/detail.
- UI hiển thị nhóm từ theo từng chữ Hán, pinyin/nghĩa/context, số lượng từ và link về detail
  chữ nguồn; không thêm blocking page header hoặc mô tả marketing.
- Responsive grid chuyển từ 3 cột → 2 cột → 1 cột ở mobile, không clipping/overflow.

### Automated test

- `npm run build` — pass.
- `git diff --check` — pass.

### Computer Use

- Cần retest route ở desktop 1440px và mobile 390px trong vòng visual regression tiếp theo.
