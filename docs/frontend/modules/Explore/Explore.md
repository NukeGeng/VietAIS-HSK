# Explore Frontend Module

Visible sidebar group: `MỞ RỘNG`.

## Screens

```text
/app/stories
/app/videos
/app/resources
/app/tools
```

## Truyện song ngữ
- list/detail;
- Chinese;
- Vietnamese;
- optional Pinyin/audio.

## Video học
- list/detail;
- player;
- transcript;
- optional Pinyin/translation.

## Tài liệu
- list/filter;
- file/link action.

## Công cụ
- list các utility đã duyệt;
- không tạo marketplace/plugin browser.

## Current implementation

- Production không fallback dữ liệu mẫu khi API lỗi; hiển thị error và nút thử lại. Request của route cũ không được ghi đè route mới.

- [x] learner list chỉ nhận extended content đã `Published` từ Content API;
- [x] Story/Video/Resource có detail route và giữ Chinese/Pinyin/Vietnamese hoặc transcript/resource link khi data có;
- [x] Tool list dùng route đã được publish;
- [x] mobile visual regression đầy đủ cho toàn bộ detail pages.

## Priority
Triển khai sau core learning flow ổn định.
