import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import PageView from './views/PageView.vue'
import LearningPathView from './views/LearningPathView.vue'
import LessonDetailView from './views/LessonDetailView.vue'
import LessonsView from './views/LessonsView.vue'
import ReviewView from './views/ReviewView.vue'
import ProgressView from './views/ProgressView.vue'
import ExamView from './views/ExamView.vue'
import PracticeView from './views/PracticeView.vue'
import ProfileView from './views/ProfileView.vue'
import FoundationView from './views/FoundationView.vue'
import TranslationView from './views/TranslationView.vue'
import SpeakingView from './views/SpeakingView.vue'
import HanziView from './views/HanziView.vue'
import VocabularyView from './views/VocabularyView.vue'
import GrammarView from './views/GrammarView.vue'

const page = (title: string, eyebrow: string, description: string) => ({
  component: PageView,
  props: { title, eyebrow, description },
  meta: { title },
})

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/app' },
    { path: '/admin', component: () => import('./views/AdminView.vue'), meta: { title: 'Quản trị' } },
    { path: '/app', component: HomeView, meta: { title: 'Trang chủ' } },
    { path: '/app/hsk', component: LearningPathView, props: { mode: 'hsk' }, meta: { title: 'Lộ trình HSK' } },
    { path: '/app/hsk/:level', component: LearningPathView, props: route => ({ mode: 'hsk', level: route.params.level as string }), meta: { title: 'Chi tiết lộ trình HSK' } },
    { path: '/app/beginner', component: LearningPathView, props: { mode: 'beginner' }, meta: { title: 'Người mới bắt đầu' } },
    { path: '/app/beginner/:stage', component: LearningPathView, props: { mode: 'beginner' }, meta: { title: 'Nền tảng Pinyin và thanh điệu' } },
    { path: '/app/lessons', component: LessonsView, meta: { title: 'Bài học' } },
    { path: '/app/lessons/:id', component: LessonDetailView, props: route => ({ id: route.params.id as string }), meta: { title: 'Chi tiết bài học' } },
    // Kỹ năng is a sidebar dropdown, not a standalone page. Keep the legacy
    // URL as a compatibility redirect so old links never render a redundant
    // page header or a dead screen.
    { path: '/app/skills', redirect: '/app/skills/listening' },
    { path: '/app/skills/speaking', component: SpeakingView, meta: { title: 'Nói - đối thoại' } },
    { path: '/app/skills/translation', component: TranslationView, meta: { title: 'Dịch Việt - Trung' } },
    { path: '/app/skills/listening', component: PracticeView, props: { type: 'listening' }, meta: { title: 'Nghe' } },
    { path: '/app/skills/reading', component: PracticeView, props: { type: 'reading' }, meta: { title: 'Đọc' } },
    { path: '/app/skills/writing', component: PracticeView, props: { type: 'writing' }, meta: { title: 'Viết' } },
    { path: '/app/skills/:skill', ...page('Kỹ năng', 'HỌC TẬP', 'Luyện một kỹ năng trong mạch học hiện tại.') },
    // Luyện tập is also a dropdown group. Deep links remain valid, while the
    // parent URL opens the first concrete practice mode instead of a duplicate
    // overview page.
    { path: '/app/practice', redirect: '/app/practice/vocabulary' },
    { path: '/app/practice/:type', component: PracticeView, props: true, meta: { title: 'Luyện tập' } },
    { path: '/app/pinyin', component: FoundationView, props: { mode: 'pinyin' }, meta: { title: 'Pinyin' } },
    { path: '/app/tones', component: FoundationView, props: { mode: 'tones' }, meta: { title: 'Thanh điệu' } },
    { path: '/app/vocabulary', component: VocabularyView, meta: { title: 'Từ vựng' } },
    { path: '/app/vocabulary/:id', component: VocabularyView, meta: { title: 'Chi tiết từ vựng' } },
    { path: '/app/hanzi', component: HanziView, props: { mode: 'list' }, meta: { title: 'Chữ Hán' } },
    { path: '/app/hanzi/strokes', component: HanziView, props: { mode: 'strokes' }, meta: { title: 'Thứ tự nét' } },
    { path: '/app/hanzi/write', component: HanziView, props: { mode: 'write' }, meta: { title: 'Luyện viết chữ Hán' } },
    { path: '/app/hanzi/related', component: HanziView, props: { mode: 'related' }, meta: { title: 'Từ liên quan' } },
    { path: '/app/hanzi/:id/strokes', component: HanziView, props: route => ({ mode: 'strokes', id: route.params.id as string }), meta: { title: 'Thứ tự nét' } },
    { path: '/app/hanzi/:id/write', component: HanziView, props: route => ({ mode: 'write', id: route.params.id as string }), meta: { title: 'Luyện viết chữ Hán' } },
    { path: '/app/hanzi/:id', component: HanziView, props: route => ({ mode: 'detail', id: route.params.id as string }), meta: { title: 'Chi tiết chữ Hán' } },
    { path: '/app/grammar', component: GrammarView, meta: { title: 'Ngữ pháp' } },
    { path: '/app/grammar/:id', component: GrammarView, meta: { title: 'Chi tiết ngữ pháp' } },
    { path: '/app/review', component: ReviewView, props: { mode: 'summary' }, meta: { title: 'Ôn tập' } },
    { path: '/app/mistakes', component: ReviewView, props: { mode: 'mistakes' }, meta: { title: 'Câu làm sai' } },
    { path: '/app/needs-review', component: ReviewView, props: { mode: 'needs-review' }, meta: { title: 'Nội dung cần ôn' } },
    { path: '/app/exams', component: ExamView, props: { mode: 'catalog' }, meta: { title: 'Thi thử' } },
    { path: '/app/exams/:examId', component: ExamView, props: route => ({ mode: 'detail', examId: route.params.examId as string }), meta: { title: 'Chi tiết đề thi' } },
    { path: '/app/exam-attempts/:attemptId', component: ExamView, props: route => ({ mode: 'attempt', attemptId: route.params.attemptId as string }), meta: { title: 'Đang thi' } },
    { path: '/app/exam-attempts/:attemptId/result', component: ExamView, props: route => ({ mode: 'result', attemptId: route.params.attemptId as string }), meta: { title: 'Kết quả thi thử' } },
    { path: '/app/progress', component: ProgressView, props: { mode: 'overview' }, meta: { title: 'Tiến độ học' } },
    { path: '/app/progress/weak-points', component: ProgressView, props: { mode: 'weak-points' }, meta: { title: 'Điểm yếu' } },
    { path: '/app/history', component: ProgressView, props: { mode: 'history' }, meta: { title: 'Lịch sử học' } },
    { path: '/app/streak', component: ProgressView, props: { mode: 'streak' }, meta: { title: 'Chuỗi ngày học' } },
    { path: '/app/stories', ...page('Truyện song ngữ', 'MỞ RỘNG', 'Nội dung mở rộng sau core learning flow.') },
    { path: '/app/stories/:id', ...page('Chi tiết truyện', 'MỞ RỘNG · TRUYỆN', 'Nội dung Trung · Pinyin · Việt đã được publish.') },
    { path: '/app/video', ...page('Video học', 'MỞ RỘNG', 'Video hỗ trợ việc học theo chủ đề.') },
    { path: '/app/video/:id', ...page('Chi tiết video', 'MỞ RỘNG · VIDEO', 'Video và transcript đã được publish.') },
    { path: '/app/resources', ...page('Tài liệu', 'MỞ RỘNG', 'Tài liệu học đã được duyệt.') },
    { path: '/app/resources/:id', ...page('Chi tiết tài liệu', 'MỞ RỘNG · TÀI LIỆU', 'Tài liệu đã được publish.') },
    { path: '/app/tools', ...page('Công cụ', 'MỞ RỘNG', 'Công cụ hỗ trợ học tiếng Trung.') },
    { path: '/app/profile', component: ProfileView, meta: { title: 'Hồ sơ' } },
    { path: '/app/settings', ...page('Cài đặt', 'TÀI KHOẢN', 'Các thiết lập cho trải nghiệm học.') },
    { path: '/app/help', ...page('Trợ giúp', 'TÀI KHOẢN', 'Hướng dẫn sử dụng VietAIS HSK 3.0.') },
  ],
  scrollBehavior: () => ({ top: 0 }),
})
