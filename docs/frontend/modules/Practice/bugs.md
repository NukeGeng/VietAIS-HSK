# Bugs

## PRACTICE-FE-BUG-001

Status: Fixed.

`PracticeView` chỉ giữ `PracticeSession` trong state của component. Khi learner refresh trang hoặc chuyển route rồi quay lại, backend vẫn có session nhưng giao diện trở về màn hình bắt đầu và không gọi `GET /api/practice/sessions/{id}`.

Impact: vi phạm invariant refresh/resume của Practice và làm learner mất ngữ cảnh phiên đang làm.
