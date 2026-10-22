# Prompt giao cho agent triển khai

> Bạn là agent chịu trách nhiệm triển khai code cho dự án `StudentManagementv2`.

## Bối cảnh

Xây dựng MVP web quản lý học sinh/học tập kiểu LMS bằng ASP.NET Core Web API .NET 5, Angular 12 và SQL Server. Database local đã tồn tại với tên `StudentManagementv2Db`. Agent khác sẽ kiểm thử sau mỗi phase; vì vậy mọi thay đổi phải có thể build, chạy và tái hiện được.

Đọc toàn bộ `docs/IMPLEMENTATION_PLAN.md` trước khi code. Dùng `docs/QA_ACCEPTANCE_CHECKLIST.md` để hiểu các kiểm thử bắt buộc.

## Nguyên tắc không được vi phạm

1. Chỉ làm việc trong `StudentManagementv2`.
2. Không drop database/table, không xóa dữ liệu hiện có, không reset database và không dùng migration phá hủy dữ liệu.
3. Trước khi tạo entity/migration, phải kiểm tra schema thực tế của `StudentManagementv2Db`, rồi ghi kết quả vào `docs/DB_SCHEMA_NOTES.md`.
4. Nếu database đã có user/role/course thì map và tái sử dụng; không seed ghi đè dữ liệu.
5. Backend phải enforce authorization theo role và ownership/scope. Angular chỉ ẩn UI, không được coi là bảo mật.
6. Không hard-code secret thật, password plaintext, đường dẫn file vật lý hoặc thông tin nhạy cảm vào git.
7. Dùng tiếng Việt cho label/message giao diện; code, route và tên field dùng tiếng Anh nhất quán.
8. Không tự ý mở rộng phạm vi ngoài MVP. Nếu schema thiếu dữ liệu cần thiết, chọn phương án ít phá vỡ nhất và ghi rõ giả định.

## Vai trò và quyền

- `STUDENT`: chỉ xem khóa đã enroll; xem nội dung, tải tài liệu, đánh dấu hoàn thành, xem/nộp bài và xem điểm/nhận xét của mình.
- `TEACHER`: chỉ quản lý khóa được phân công; quản lý chương/bài/tài liệu/bài tập; xem và chấm bài nộp trong khóa của mình.
- `ADMIN`: toàn quyền trong phạm vi MVP.

## Cách làm bắt buộc

- Thực hiện đúng 3 phase bên dưới, không gộp phase.
- Trước mỗi phase: nêu mục tiêu, file dự kiến sửa/tạo và rủi ro.
- Sau mỗi phase: chạy build/test, cập nhật tài liệu, báo cáo kết quả và dừng để chờ xác nhận chuyển phase.
- Nếu gặp lỗi schema hoặc thiếu thông tin, kiểm tra read-only và đưa ra giả định có thể đảo ngược; không tự ý phá dữ liệu.
- Controller mỏng, logic ở service, DTO không expose dữ liệu nhạy cảm.
- Dùng async EF Core, validate request, trả HTTP status code rõ ràng.

## Phase 1 - Foundation, authentication, authorization, course read

### Backend

- Tạo solution/API .NET 5 và cấu hình EF Core 5 SQL Server.
- Kết nối database `StudentManagementv2Db` bằng connection string cấu hình được.
- Kiểm tra và map schema, tạo `docs/DB_SCHEMA_NOTES.md`.
- Implement JWT login, current user, logout/session behavior, password hash.
- Claims/authorization cho `STUDENT`, `TEACHER`, `ADMIN`.
- Implement course list/detail theo scope:
  - Student: chỉ enrolled courses.
  - Teacher: chỉ assigned courses.
  - Admin: tất cả.
- Bật Swagger và CORS cho Angular local.

### Frontend

- Khởi tạo Angular 12.
- Login/logout, lưu/khôi phục session, HTTP interceptor, auth guard, role guard.
- Trang danh sách khóa học, chi tiết khóa học và layout/menu theo role.
- Có trạng thái loading, empty, error, 401, 403.

### Bắt buộc pass trước khi dừng phase 1

- `dotnet build` pass.
- `ng build` pass.
- Login đúng/sai xử lý đúng.
- Student/Teacher/Admin không vượt được phạm vi bằng cách đổi URL hoặc ID request.
- Có tài liệu endpoint và hướng dẫn chạy.

## Phase 2 - Course content, resources, learning progress

### Backend

- CRUD chapter/lesson với thứ tự và ownership.
- Lesson detail có video, tài liệu và progress.
- Upload/download tài liệu với giới hạn extension/MIME/size, tên file an toàn và storage path cấu hình được.
- Student đánh dấu lesson complete idempotent.

### Frontend

- Learning page có sidebar chapter/lesson, lesson detail, video, resource download.
- Hiển thị progress và trạng thái hoàn thành sau reload.
- Teacher UI quản lý chapter/lesson/resource.

### Bắt buộc pass trước khi dừng phase 2

- Nội dung đúng thứ tự.
- Student không đọc được lesson/resource ngoài khóa của mình.
- File hợp lệ upload/download được; file sai bị reject.
- Progress được lưu và không tạo bản ghi trùng khi gọi lại.
- Regression phase 1 pass.

## Phase 3 - Assignment, submission, grading

### Backend

- Teacher/Admin CRUD assignment, có due date và max score.
- Student xem assignment của khóa đã enroll.
- Student nộp text/file; server ghi `submittedAt` và tính status on-time/late.
- Teacher xem submissions của khóa được giao, chấm điểm và feedback.
- Student chỉ xem submission/điểm của chính mình.
- Validate điểm trong `[0, maxScore]`.

### Frontend

- Assignment list/detail, due date/status, form nộp text/file.
- Hiển thị đã nộp, trễ hạn, điểm và feedback.
- Teacher grading screen.

### Bắt buộc pass trước khi bàn giao MVP

- Không thể bypass scope bằng sửa URL/body.
- Client chỉnh đồng hồ không làm sai trạng thái late.
- Điểm invalid bị reject.
- Regression phase 1 và 2 pass.
- `dotnet build`, `ng build` và test tự động pass.

## Định dạng báo cáo sau mỗi phase

```text
Phase: <1|2|3>
Status: DONE | BLOCKED
Files changed:
- ...
Implemented:
- ...
Database/API changes:
- ...
Commands run and results:
- ...
Manual test data:
- ...
Known issues:
- ...
Next step:
- Chờ xác nhận phase tiếp theo
```

Bắt đầu bằng việc khảo sát thư mục và schema database, không code trước bước khảo sát đó.
