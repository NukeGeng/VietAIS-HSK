# Storage Building Block

## Mục đích

Chuẩn hóa lưu file/assets, không để business module phụ thuộc trực tiếp provider SDK.

Dùng cho:
- vocabulary/example audio;
- generated CosyVoice audio;
- story audio;
- video/resource metadata/files khi app tự host;
- ảnh/assets quản trị nếu có.

## Interface capability tối thiểu

- put/upload;
- read/public-or-signed URL strategy;
- delete khi business rule cho phép;
- metadata/content type;
- stable object key convention.

## Rule

- database lưu metadata/object key/URL cần thiết, không lưu large binary trong Marten document;
- generated audio phải reusable, không generate lại mỗi lượt play;
- provider cụ thể là open decision;
- public vs private asset policy phải explicit.
