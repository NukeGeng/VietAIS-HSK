# Fixed

- `LEARNING-FE-FIX-001` — HomeView có trạng thái loading/error rõ ràng, không render dashboard giả khi API chưa trả dữ liệu.
- `LEARNING-FE-FIX-002` — Dashboard gọi `/api/learning/home` và `/api/progress` song song; nút tiếp tục có fallback về lộ trình HSK hoặc beginner.
- `LEARNING-FE-FIX-003` — Trang Lộ trình HSK và Người mới bắt đầu đã gọi API thật, hiển thị loading/error/empty, start beginner và danh sách 7 stage nền tảng.
- Computer Use: `/app/hsk`, `/app/beginner`, start beginner — pass.
