# QA acceptance checklist cho MVP

Tài liệu này dùng cho agent kiểm thử sau khi agent triển khai bàn giao từng phase. Mỗi mục cần ghi `PASS`, `FAIL` hoặc `BLOCKED`, kèm endpoint, user test và bằng chứng ngắn.

## 0. Chuẩn bị

- [x] Có `docs/DB_SCHEMA_NOTES.md` và `docs/API_CONTRACT.md`.
- [x] Biết lệnh chạy API và Angular.
- [x] Database test là `StudentManagementv2Db` local; xác nhận không dùng nhầm database khác.
- [x] Có ít nhất một tài khoản cho mỗi role: `STUDENT`, `TEACHER`, `ADMIN`.
- [x] Có tối thiểu: 2 course, 2 teacher, 2 student; trong đó mỗi student/teacher có scope khác nhau.
- [x] Có backup hoặc dữ liệu test có thể khôi phục trước các test ghi dữ liệu.
- [x] Chạy `dotnet build` và `ng build` trước khi test chức năng.

## 1. Authentication và session

- [x] Login đúng trả token, user profile và role.
- [x] Login sai password không tiết lộ user tồn tại hay không.
- [x] Request không token trả `401`.
- [x] Token không hợp lệ/hết hạn trả `401` và frontend đưa về login.
- [x] Refresh trang khi token còn hạn vẫn giữ session.
- [x] Logout xóa session/token phía client và không gọi được endpoint protected.
- [x] Password/JWT không xuất hiện trong UI log hoặc response thừa.

## 2. Authorization và scope - ưu tiên cao

- [x] Student chỉ thấy course đã enroll.
- [x] Student gọi trực tiếp `GET /courses/{otherCourseId}` nhận `403` hoặc `404` nhất quán.
- [x] Student không truy cập chapter, lesson, resource, assignment hoặc submission ngoài scope.
- [x] Teacher chỉ thấy course được assign.
- [x] Teacher không sửa/xóa course của teacher khác.
- [x] Teacher không quản lý nội dung/assignment ngoài course được assign.
- [x] Teacher không xem/chấm submission ngoài course được assign.
- [x] Admin thực hiện được thao tác được quy định trong MVP.
- [x] Chỉ ẩn nút trên UI không được tính là pass; phải test bằng request trực tiếp/Swagger.

## 3. Course và content

- [x] Course list có title/code/teacher/description phù hợp.
- [x] Course detail tải đúng và không lộ field nội bộ.
- [x] Chapter/lesson sắp xếp đúng theo order.
- [x] Teacher tạo/sửa/xóa được content trong course của mình.
- [x] Teacher không tạo/sửa/xóa content ngoài scope.
- [x] Student xem được nội dung được publish.
- [x] Video URL/path hoạt động hoặc có thông báo lỗi rõ ràng.
- [x] Tài liệu PDF/file hợp lệ tải xuống được qua endpoint có kiểm tra quyền.
- [x] File sai extension/MIME/quá lớn bị reject.
- [x] Không thể dùng path traversal hoặc tên file đặc biệt để đọc/ghi file ngoài storage root.

## 4. Lesson progress

- [x] Student đánh dấu hoàn thành thành công.
- [x] Gọi thao tác complete nhiều lần không tạo dữ liệu trùng/lỗi.
- [x] Reload vẫn hiển thị đúng trạng thái completed.
- [x] Student không thể ghi progress cho lesson ngoài scope.
- [x] Teacher/Admin xem được thông tin theo phạm vi được thiết kế.

## 5. Assignment và submission

- [x] Teacher tạo assignment với title, description, due date, max score.
- [x] Due date invalid bị reject.
- [x] Student chỉ thấy assignment của course đã enroll.
- [x] Student nộp text thành công.
- [x] Student nộp file thành công nếu file hợp lệ.
- [x] Submission được gắn đúng student hiện tại từ token, không tin `studentId` do client gửi.
- [x] Submission trước hoặc đúng hạn có status on-time.
- [x] Submission sau hạn có status late theo server time.
- [x] Client chỉnh đồng hồ không thay đổi kết quả late.
- [x] Hành vi nộp lại đúng tài liệu: update submission hiện tại hoặc tạo version theo đặc tả.
- [x] Student chỉ xem submission/điểm/feedback của chính mình.
- [x] Teacher xem danh sách submission của course mình.
- [x] Teacher chấm điểm hợp lệ và feedback được lưu.
- [x] Điểm âm hoặc lớn hơn max score bị reject.
- [x] Teacher ngoài scope không xem/chấm được submission.
- [x] Student thấy điểm/feedback sau khi teacher chấm.

## 6. Frontend UX và lỗi

- [x] Route protected redirect đúng khi chưa login.
- [x] Menu/action hiển thị theo role.
- [x] Có loading state khi gọi API.
- [x] Có empty state khi danh sách rỗng.
- [x] Có thông báo dễ hiểu cho 400/401/403/404/409/413/500.
- [x] Form bắt buộc và giới hạn điểm/file được validate ở UI lẫn backend.
- [x] Layout dùng được ở desktop và viewport hẹp cơ bản.
- [x] Không có lỗi nghiêm trọng trong browser console khi chạy happy path.

## 7. Regression và chất lượng code

- [x] `dotnet build` pass.
- [x] `ng build` pass.
- [x] Unit/integration tests hiện có pass (21/21 tests pass).
- [x] API không dùng query nối chuỗi từ input.
- [x] Không có secret/password/file runtime trong git diff.
- [x] Không có thay đổi destructive với database.
- [x] Swagger phản ánh endpoint thực tế.
- [x] Tài liệu chạy dự án cập nhật đúng port, connection string và test data.

## 8. Báo cáo test

```text
Phase: <1|2|3>
Result: PASS | FAIL | BLOCKED
Environment:
- API:
- Frontend:
- Database:

Passed:
- ...

Failed:
- Test ID:
- Steps:
- Expected:
- Actual:
- Evidence:

Security findings:
- ...

Regression result:
- ...

Recommendation:
- Approve phase / Fix and retest / Block release
```
