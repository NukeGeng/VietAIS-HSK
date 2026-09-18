import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import PageView from './views/PageView.vue'
import LearningPathView from './views/LearningPathView.vue'
import ReviewView from './views/ReviewView.vue'
import ProgressView from './views/ProgressView.vue'
import ExamView from './views/ExamView.vue'
import PracticeView from './views/PracticeView.vue'
import ProfileView from './views/ProfileView.vue'

const page = (title: string, eyebrow: string, description: string) => ({
  component: PageView,
  props: { title, eyebrow, description },
})

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/app' },
    { path: '/app', component: HomeView, meta: { title: 'Trang chủ' } },
    { path: '/app/hsk', component: LearningPathView, props: { mode: 'hsk' }, meta: { title: 'Lộ trình HSK' } },
    { path: '/app/hsk/:level', ...page('Chi tiết lộ trình HSK', 'ĐANG HỌC', 'Theo dõi topic, unit và lesson theo cấp độ.') },
    { path: '/app/beginner', component: LearningPathView, props: { mode: 'beginner' }, meta: { title: 'Người mới bắt đầu' } },
    { path: '/app/beginner/:stage', component: LearningPathView, props: { mode: 'beginner' }, meta: { title: 'Nền tảng Pinyin và thanh điệu' } },
    { path: '/app/lessons', ...page('Bài học', 'HỌC TẬP', 'Các bài học theo chủ đề, unit và lộ trình hiện tại.') },
    { path: '/app/lessons/:id', ...page('Chi tiết bài học', 'BÀI HỌC', 'Nội dung bài học và các mục tiêu cần hoàn thành.') },
    { path: '/app/skills', ...page('Kỹ năng', 'HỌC TẬP', 'Nghe, đọc, viết, nói và dịch theo context HSK.') },
    { path: '/app/skills/:skill', ...page('Kỹ năng', 'HỌC TẬP', 'Luyện một kỹ năng trong mạch học hiện tại.') },
    { path: '/app/practice', component: PracticeView, meta: { title: 'Luyện tập' } },
    { path: '/app/practice/:type', component: PracticeView, props: true, meta: { title: 'Luyện tập' } },
    { path: '/app/pinyin', ...page('Pinyin', 'NỀN TẢNG', 'Học và tra cứu âm tiết tiếng Trung.') },
    { path: '/app/tones', ...page('Thanh điệu', 'NỀN TẢNG', 'Nhận diện và luyện thanh điệu.') },
    { path: '/app/vocabulary', ...page('Từ vựng', 'NỀN TẢNG', 'Tra cứu từ vựng, Pinyin, nghĩa và ví dụ.') },
    { path: '/app/hanzi', ...page('Chữ Hán', 'NỀN TẢNG', 'Tra cứu chữ, bộ thủ, số nét và từ liên quan.') },
    { path: '/app/hanzi/strokes', ...page('Thứ tự nét', 'CHỮ HÁN', 'Xem từng nét và thứ tự viết của chữ.') },
    { path: '/app/hanzi/write', ...page('Luyện viết chữ Hán', 'CHỮ HÁN', 'Luyện viết theo hướng dẫn hoặc tự nhớ.') },
    { path: '/app/hanzi/related', ...page('Từ liên quan', 'CHỮ HÁN', 'Các từ liên quan đến chữ đang học.') },
    { path: '/app/grammar', ...page('Ngữ pháp', 'NỀN TẢNG', 'Tra cứu mẫu câu và giải thích đơn giản.') },
    { path: '/app/review', component: ReviewView, props: { mode: 'summary' }, meta: { title: 'Ôn tập' } },
    { path: '/app/mistakes', component: ReviewView, props: { mode: 'mistakes' }, meta: { title: 'Câu làm sai' } },
    { path: '/app/needs-review', component: ReviewView, props: { mode: 'needs-review' }, meta: { title: 'Nội dung cần ôn' } },
    { path: '/app/exams', component: ExamView, meta: { title: 'Thi thử' } },
    { path: '/app/progress', component: ProgressView, props: { mode: 'overview' }, meta: { title: 'Tiến độ học' } },
    { path: '/app/progress/weak-points', component: ProgressView, props: { mode: 'weak-points' }, meta: { title: 'Điểm yếu' } },
    { path: '/app/history', component: ProgressView, props: { mode: 'history' }, meta: { title: 'Lịch sử học' } },
    { path: '/app/streak', component: ProgressView, props: { mode: 'streak' }, meta: { title: 'Chuỗi ngày học' } },
    { path: '/app/stories', ...page('Truyện song ngữ', 'MỞ RỘNG', 'Nội dung mở rộng sau core learning flow.') },
    { path: '/app/video', ...page('Video học', 'MỞ RỘNG', 'Video hỗ trợ việc học theo chủ đề.') },
    { path: '/app/resources', ...page('Tài liệu', 'MỞ RỘNG', 'Tài liệu học đã được duyệt.') },
    { path: '/app/tools', ...page('Công cụ', 'MỞ RỘNG', 'Công cụ hỗ trợ học tiếng Trung.') },
    { path: '/app/profile', component: ProfileView, meta: { title: 'Hồ sơ' } },
    { path: '/app/settings', ...page('Cài đặt', 'TÀI KHOẢN', 'Các thiết lập cho trải nghiệm học.') },
    { path: '/app/help', ...page('Trợ giúp', 'TÀI KHOẢN', 'Hướng dẫn sử dụng VietAIS HSK 3.0.') },
  ],
  scrollBehavior: () => ({ top: 0 }),
})
