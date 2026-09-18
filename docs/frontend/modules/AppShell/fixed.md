# Fixed

- `APPSHELL-FIX-001` — Giảm độ âm của `letter-spacing` và tăng `line-height` cho heading/card heading để tiếng Việt không bị dính chữ. Đã xác nhận lại bằng Computer Use trên route `/app`.
- `APPSHELL-FIX-002` — AppShell Vue có sidebar responsive, parent menu/dropdown, active route, mobile drawer/backdrop và nhóm điều hướng theo navigation map trong docs.
- `APPSHELL-FIX-003` — Thêm route fallback `/app/beginner/:stage` để `continueTarget` từ Learning không tạo dead link.
- `APPSHELL-FIX-004` — Active state của Trang chủ chỉ khớp chính xác `/app`; các route con không còn làm mất trạng thái active của mục hiện tại. Đã retest bằng Computer Use.
- `APPSHELL-FIX-005` — Đồng bộ AppShell Vue với `design-template` panda theme: dùng logo asset thật, token xám/đen, cấu trúc sidebar/header/app overview và breakpoint drawer 820px; giữ nguyên API và route behavior. Đã đối chiếu Computer Use ở viewport desktop và breakpoint mobile.
