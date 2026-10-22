# QA acceptance checklist cho MVP

Tài liệu này dùng cho agent kiểm thử sau khi agent triển khai bàn giao từng phase. Mỗi mục cần ghi `PASS`, `FAIL` hoặc `BLOCKED`, kèm endpoint, user test và bằng chứng ngắn.

## 0. Chuẩn bị

- [ ] Có `docs/DB_SCHEMA_NOTES.md` và `docs/API_CONTRACT.md`.
- [ ] Biết lệnh chạy API và Angular.
- [ ] Database test là `StudentManagementv2Db` local; xác nhận không dùng nhầm database khác.
- [ ] Có ít nhất một tài khoản cho mỗi role: `STUDENT`, `TEACHER`, `ADMIN`.
- [ ] Có tối thiểu: 2 course, 2 teacher, 2 student; trong đó mỗi student/teacher có scope khác nhau.
- [ ] Có backup hoặc dữ liệu test có thể khôi phục trước các test ghi dữ liệu.
- [ ] Chạy `dotnet build` và `ng build` trước khi test chức năng.

## 1. Authentication và session

- [ ] Login đúng trả token, user profile và role.
- [ ] Login sai password không tiết lộ user tồn tại hay không.
- [ ] Request không token trả `401`.
- [ ] Token không hợp lệ/hết hạn trả `401` và frontend đưa về login.
- [ ] Refresh trang khi token còn hạn vẫn giữ session.
- [ ] Logout xóa session/token phía client và không gọi được endpoint protected.
- [ ] Password/JWT không xuất hiện trong UI log hoặc response thừa.

## 2. Authorization và scope - ưu tiên cao

- [ ] Student chỉ thấy course đã enroll.
- [ ] Student gọi trực tiếp `GET /courses/{otherCourseId}` nhận `403` hoặc `404` nhất quán.
- [ ] Student không truy cập chapter, lesson, resource, assignment hoặc submission ngoài scope.
- [ ] Teacher chỉ thấy course được assign.
- [ ] Teacher không sửa/xóa course của teacher khác.
- [ ] Teacher không quản lý nội dung/assignment ngoài course được assign.
- [ ] Teacher không xem/chấm submission ngoài course được assign.
- [ ] Admin thực hiện được thao tác được quy định trong MVP.
- [ ] Chỉ ẩn nút trên UI không được tính là pass; phải test bằng request trực tiếp/Swagger.

## 3. Course và content

- [ ] Course list có title/code/teacher/description phù hợp.
- [ ] Course detail tải đúng và không lộ field nội bộ.
- [ ] Chapter/lesson sắp xếp đúng theo order.
- [ ] Teacher tạo/sửa/xóa được content trong course của mình.
- [ ] Teacher không tạo/sửa/xóa content ngoài scope.
- [ ] Student xem được nội dung được publish.
- [ ] Video URL/path hoạt động hoặc có thông báo lỗi rõ ràng.
- [ ] Tài liệu PDF/file hợp lệ tải xuống được qua endpoint có kiểm tra quyền.
- [ ] File sai extension/MIME/quá lớn bị reject.
- [ ] Không thể dùng path traversal hoặc tên file đặc biệt để đọc/ghi file ngoài storage root.

## 4. Lesson progress

- [ ] Student đánh dấu hoàn thành thành công.
- [ ] Gọi thao tác complete nhiều lần không tạo dữ liệu trùng/lỗi.
- [ ] Reload vẫn hiển thị đúng trạng thái completed.
- [ ] Student không thể ghi progress cho lesson ngoài scope.
- [ ] Teacher/Admin xem được thông tin theo phạm vi được thiết kế.

## 5. Assignment và submission

- [ ] Teacher tạo assignment với title, description, due date, max score.
- [ ] Due date invalid bị reject.
- [ ] Student chỉ thấy assignment của course đã enroll.
- [ ] Student nộp text thành công.
- [ ] Student nộp file thành công nếu file hợp lệ.
- [ ] Submission được gắn đúng student hiện tại từ token, không tin `studentId` do client gửi.
- [ ] Submission trước hoặc đúng hạn có status on-time.
- [ ] Submission sau hạn có status late theo server time.
- [ ] Client chỉnh đồng hồ không thay đổi kết quả late.
- [ ] Hành vi nộp lại đúng tài liệu: update submission hiện tại hoặc tạo version theo đặc tả.
- [ ] Student chỉ xem submission/điểm/feedback của chính mình.
- [ ] Teacher xem danh sách submission của course mình.
- [ ] Teacher chấm điểm hợp lệ và feedback được lưu.
- [ ] Điểm âm hoặc lớn hơn max score bị reject.
- [ ] Teacher ngoài scope không xem/chấm được submission.
- [ ] Student thấy điểm/feedback sau khi teacher chấm.

## 6. Frontend UX và lỗi

- [ ] Route protected redirect đúng khi chưa login.
- [ ] Menu/action hiển thị theo role.
- [ ] Có loading state khi gọi API.
- [ ] Có empty state khi danh sách rỗng.
- [ ] Có thông báo dễ hiểu cho 400/401/403/404/409/413/500.
- [ ] Form bắt buộc và giới hạn điểm/file được validate ở UI lẫn backend.
- [ ] Layout dùng được ở desktop và viewport hẹp cơ bản.
- [ ] Không có lỗi nghiêm trọng trong browser console khi chạy happy path.

## 7. Regression và chất lượng code

- [ ] `dotnet build` pass.
- [ ] `ng build` pass.
- [ ] Unit/integration tests hiện có pass.
- [ ] API không dùng query nối chuỗi từ input.
- [ ] Không có secret/password/file runtime trong git diff.
- [ ] Không có thay đổi destructive với database.
- [ ] Swagger phản ánh endpoint thực tế.
- [ ] Tài liệu chạy dự án cập nhật đúng port, connection string và test data.

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
