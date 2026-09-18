import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import PageView from './views/PageView.vue'

const page = (title: string, eyebrow: string, description: string) => ({
  component: PageView,
  props: { title, eyebrow, description },
})

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/app' },
    { path: '/app', component: HomeView, meta: { title: 'Trang chủ' } },
    { path: '/app/hsk', ...page('Lộ trình HSK', 'ĐANG HỌC', 'Theo dõi cấp độ hiện tại và bài học tiếp theo.') },
    { path: '/app/beginner', ...page('Người mới bắt đầu', 'NỀN TẢNG', 'Bắt đầu từ Pinyin, thanh điệu và những bước đầu tiên.') },
    { path: '/app/beginner/:stage', ...page('Nền tảng Pinyin và thanh điệu', 'NỀN TẢNG', 'Học từng bước từ âm tiết, thanh điệu đến bài luyện đầu tiên.') },
    { path: '/app/lessons', ...page('Bài học', 'HỌC TẬP', 'Các bài học theo chủ đề, unit và lộ trình hiện tại.') },
    { path: '/app/lessons/:id', ...page('Chi tiết bài học', 'BÀI HỌC', 'Nội dung bài học và các mục tiêu cần hoàn thành.') },
    { path: '/app/skills', ...page('Kỹ năng', 'HỌC TẬP', 'Nghe, đọc, viết, nói và dịch theo context HSK.') },
    { path: '/app/skills/:skill', ...page('Kỹ năng', 'HỌC TẬP', 'Luyện một kỹ năng trong mạch học hiện tại.') },
    { path: '/app/practice', ...page('Luyện tập', 'HỌC TẬP', 'Chọn dạng bài và luyện tập theo mục tiêu.') },
    { path: '/app/practice/:type', ...page('Luyện tập', 'HỌC TẬP', 'Làm bài, nhận feedback và tiếp tục.') },
    { path: '/app/pinyin', ...page('Pinyin', 'NỀN TẢNG', 'Học và tra cứu âm tiết tiếng Trung.') },
    { path: '/app/tones', ...page('Thanh điệu', 'NỀN TẢNG', 'Nhận diện và luyện thanh điệu.') },
    { path: '/app/vocabulary', ...page('Từ vựng', 'NỀN TẢNG', 'Tra cứu từ vựng, Pinyin, nghĩa và ví dụ.') },
    { path: '/app/hanzi', ...page('Chữ Hán', 'NỀN TẢNG', 'Tra cứu chữ, bộ thủ, số nét và từ liên quan.') },
    { path: '/app/hanzi/strokes', ...page('Thứ tự nét', 'CHỮ HÁN', 'Xem từng nét và thứ tự viết của chữ.') },
    { path: '/app/hanzi/write', ...page('Luyện viết chữ Hán', 'CHỮ HÁN', 'Luyện viết theo hướng dẫn hoặc tự nhớ.') },
    { path: '/app/hanzi/related', ...page('Từ liên quan', 'CHỮ HÁN', 'Các từ liên quan đến chữ đang học.') },
    { path: '/app/grammar', ...page('Ngữ pháp', 'NỀN TẢNG', 'Tra cứu mẫu câu và giải thích đơn giản.') },
    { path: '/app/review', ...page('Ôn tập', 'ÔN TẬP', 'Những nội dung đến hạn trong hôm nay.') },
    { path: '/app/mistakes', ...page('Câu làm sai', 'ÔN TẬP', 'Xem lại các câu cần làm lại.') },
    { path: '/app/needs-review', ...page('Nội dung cần ôn', 'ÔN TẬP', 'Từ, chữ Hán và ngữ pháp cần củng cố.') },
    { path: '/app/exams', ...page('Thi thử', 'KIỂM TRA', 'Chọn một đề thi và bắt đầu khi sẵn sàng.') },
    { path: '/app/progress', ...page('Tiến độ học', 'TIẾN ĐỘ', 'Theo dõi tiến độ vừa đủ, tập trung vào hành động tiếp theo.') },
    { path: '/app/progress/weak-points', ...page('Điểm yếu', 'TIẾN ĐỘ', 'Mỗi điểm yếu đều có hành động luyện lại hoặc ôn ngay.') },
    { path: '/app/history', ...page('Lịch sử học', 'TIẾN ĐỘ', 'Các hoạt động học có ý nghĩa theo thời gian.') },
    { path: '/app/streak', ...page('Chuỗi ngày học', 'TIẾN ĐỘ', 'Những ngày có hoạt động học được ghi nhận.') },
    { path: '/app/stories', ...page('Truyện song ngữ', 'MỞ RỘNG', 'Nội dung mở rộng sau core learning flow.') },
    { path: '/app/video', ...page('Video học', 'MỞ RỘNG', 'Video hỗ trợ việc học theo chủ đề.') },
    { path: '/app/resources', ...page('Tài liệu', 'MỞ RỘNG', 'Tài liệu học đã được duyệt.') },
    { path: '/app/tools', ...page('Công cụ', 'MỞ RỘNG', 'Công cụ hỗ trợ học tiếng Trung.') },
    { path: '/app/profile', ...page('Hồ sơ', 'TÀI KHOẢN', 'Thông tin learner và thiết lập học cơ bản.') },
    { path: '/app/settings', ...page('Cài đặt', 'TÀI KHOẢN', 'Các thiết lập cho trải nghiệm học.') },
    { path: '/app/help', ...page('Trợ giúp', 'TÀI KHOẢN', 'Hướng dẫn sử dụng VietAIS HSK 3.0.') },
  ],
  scrollBehavior: () => ({ top: 0 }),
})
