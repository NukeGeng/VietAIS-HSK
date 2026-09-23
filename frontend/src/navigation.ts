import type { NavGroup } from './types'

export const navigation: NavGroup[] = [
  {
    label: 'TỔNG QUAN',
    items: [
      { label: 'Trang chủ', to: '/app' },
      { label: 'Lộ trình HSK', to: '/app/hsk' },
      { label: 'Người mới bắt đầu', to: '/app/beginner' },
    ],
  },
  {
    label: 'HỌC TẬP',
    items: [
      { label: 'Bài học', to: '/app/lessons' },
      {
        label: 'Kỹ năng',
        children: [
          { label: 'Nghe', to: '/app/skills/listening' },
          { label: 'Đọc', to: '/app/skills/reading' },
          { label: 'Viết', to: '/app/skills/writing' },
          { label: 'Nói - đối thoại', to: '/app/skills/speaking' },
          { label: 'Dịch Việt - Trung', to: '/app/skills/translation' },
        ],
      },
      {
        label: 'Luyện tập',
        children: [
          { label: 'Từ vựng', to: '/app/practice/vocabulary' },
          { label: 'Chữ Hán', to: '/app/practice/hanzi' },
          { label: 'Ngữ pháp', to: '/app/practice/grammar' },
          { label: 'Nghe', to: '/app/practice/listening' },
          { label: 'Đọc', to: '/app/practice/reading' },
          { label: 'Viết', to: '/app/practice/writing' },
        ],
      },
    ],
  },
  {
    label: 'NỀN TẢNG',
    items: [
      { label: 'Pinyin', to: '/app/pinyin' },
      { label: 'Thanh điệu', to: '/app/tones' },
      { label: 'Từ vựng', to: '/app/vocabulary' },
      {
        label: 'Chữ Hán',
        to: '/app/hanzi',
        children: [
          { label: 'Danh sách chữ', to: '/app/hanzi' },
          { label: 'Thứ tự nét', to: '/app/hanzi/strokes' },
          { label: 'Luyện viết', to: '/app/hanzi/write' },
          { label: 'Từ liên quan', to: '/app/hanzi/related' },
        ],
      },
      { label: 'Ngữ pháp', to: '/app/grammar' },
    ],
  },
  {
    label: 'ÔN TẬP & KIỂM TRA',
    items: [
      { label: 'Ôn tập', to: '/app/review' },
      { label: 'Câu làm sai', to: '/app/mistakes' },
      { label: 'Nội dung cần ôn', to: '/app/needs-review' },
      { label: 'Thi thử', to: '/app/exams' },
    ],
  },
  {
    label: 'TIẾN ĐỘ',
    items: [
      { label: 'Tiến độ học', to: '/app/progress' },
      { label: 'Điểm yếu', to: '/app/progress/weak-points' },
      { label: 'Lịch sử học', to: '/app/history' },
      { label: 'Chuỗi ngày học', to: '/app/streak' },
    ],
  },
  {
    label: 'MỞ RỘNG',
    items: [
      { label: 'Truyện song ngữ', to: '/app/stories' },
      { label: 'Video học', to: '/app/video' },
      { label: 'Tài liệu', to: '/app/resources' },
      { label: 'Công cụ', to: '/app/tools' },
    ],
  },
  {
    label: 'TÀI KHOẢN',
    items: [
      { label: 'Hồ sơ', to: '/app/profile' },
      { label: 'Cài đặt', to: '/app/settings' },
      { label: 'Trợ giúp', to: '/app/help' },
    ],
  },
]
