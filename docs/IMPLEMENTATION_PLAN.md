# Kế hoạch triển khai MVP StudentManagementv2

## 1. Mục tiêu

Xây dựng MVP web quản lý học sinh/học tập có trải nghiệm gần với một LMS đại học: người dùng đăng nhập, được phân quyền theo vai trò, xem khóa học được phép truy cập, học nội dung theo chương/bài và xử lý bài tập - nộp bài - chấm điểm.

MVP phải chạy được end-to-end trên máy local với:

- ASP.NET Core Web API .NET 5.
- Entity Framework Core 5 + SQL Server.
- Angular 12 + TypeScript + RxJS.
- Database có sẵn tên `StudentManagementv2Db`.

## 2. Phạm vi và quy tắc nghiệp vụ

### Vai trò

| Vai trò | Quyền MVP |
|---|---|
| `STUDENT` | Đăng nhập/đăng xuất; chỉ xem khóa học đã được enroll; xem thông tin, chương, bài, tài liệu/video; đánh dấu hoàn thành; xem và nộp bài; xem trạng thái, điểm và nhận xét của bài đã chấm. |
| `TEACHER` | Đăng nhập/đăng xuất; chỉ xem và quản lý khóa học được phân công; tạo/sửa/xóa khóa học của mình; quản lý chương, bài học, tài liệu; tạo/sửa/xóa bài tập; xem bài nộp của sinh viên trong khóa học; chấm điểm và nhận xét. |
| `ADMIN` | Toàn quyền MVP ở cấp hệ thống; xem/quản lý mọi khóa học và tài khoản theo phạm vi được triển khai; có thể hỗ trợ kiểm tra dữ liệu. Không được bỏ qua kiểm tra quyền ở backend. |

### Invariants bắt buộc

1. Mọi endpoint nghiệp vụ, trừ login, phải yêu cầu JWT hợp lệ.
2. Backend phải kiểm tra quyền và quyền sở hữu trong service/query; ẩn nút ở Angular chỉ là UX, không phải cơ chế bảo mật.
3. `STUDENT` không được đọc khóa học mà mình chưa enroll bằng cách đổi `courseId` trên URL/API.
4. `TEACHER` không được sửa/xóa tài nguyên của khóa học mà mình không được phân công.
5. Hạn nộp được so sánh ở server theo UTC; response phải hiển thị rõ trạng thái `NOT_SUBMITTED`, `SUBMITTED_ON_TIME`, `SUBMITTED_LATE` hoặc tương đương.
6. Mỗi sinh viên chỉ có một bài nộp hiện hành cho một bài tập trong MVP; nếu cho phép nộp lại thì phải quy định rõ lần nộp cuối là bản được chấm.
7. Không log mật khẩu, JWT hoặc nội dung file nhạy cảm.
8. Không seed tài khoản/mật khẩu mặc định vào database local hiện có nếu chưa có cơ chế rõ ràng và không ghi đè dữ liệu.

## 3. Kiểm tra database trước khi code

Agent triển khai phải làm việc này đầu tiên và ghi kết quả vào `docs/DB_SCHEMA_NOTES.md`:

1. Xác nhận SQL Server instance, database `StudentManagementv2Db` và quyền kết nối.
2. Liệt kê tables, columns, primary keys, foreign keys, indexes và các bảng/field liên quan đến user, role, student, teacher, course, enrollment.
3. Đối chiếu schema thực tế với mô hình MVP bên dưới.
4. Ưu tiên map vào bảng hiện có bằng EF Core Fluent API hoặc database-first scaffolding.
5. Nếu thiếu bảng MVP, tạo script SQL tăng dần, có kiểm tra `IF NOT EXISTS`; không drop/rename/alter dữ liệu hiện có một cách không reversible.
6. Nếu schema hiện có khác tên, giữ tên database hiện có và ghi mapping rõ trong `DB_SCHEMA_NOTES.md`.
7. Connection string dùng `StudentManagementv2Db`; giá trị cụ thể của server/instance phải lấy từ môi trường local, không hard-code thông tin bí mật vào source control.

### Mô hình dữ liệu logic tối thiểu

Tên bảng/entity có thể điều chỉnh theo schema thực tế, nhưng phải thể hiện được các quan hệ sau:

```text
User 1---N UserRole N---1 Role
User 1---0..1 StudentProfile
User 1---0..1 TeacherProfile
Course N---1 Teacher
Course 1---N CourseChapter 1---N Lesson
Course N---N Student thông qua Enrollment
Lesson 1---N LessonResource
Course 1---N Assignment 1---N Submission N---1 Student
Lesson N---N Student thông qua LessonProgress
```

Các field nghiệp vụ tối thiểu:

- `User`: id, username/email, password hash, display name, active, created time.
- `Role`: một trong `STUDENT`, `TEACHER`, `ADMIN`.
- `Course`: code, name/title, description, teacher id, status, created/updated time.
- `CourseChapter`: course id, title, description, order.
- `Lesson`: chapter id, title, description, order, video URL hoặc video path, published.
- `LessonResource`: lesson id, file name, file URL/path, content type, size.
- `Enrollment`: course id, student id, status, enrolled time.
- `LessonProgress`: lesson id, student id, completed, completed time.
- `Assignment`: course id hoặc lesson id, title, description, due time, max score, active.
- `Submission`: assignment id, student id, text content, file metadata/path, submitted time, score, feedback, graded time.

Nếu file upload chưa thể lưu ở cloud, MVP dùng thư mục local cấu hình được, ví dụ `Storage:RootPath`; database chỉ lưu metadata và relative path. Không lưu binary lớn trực tiếp vào response JSON.

## 4. Kiến trúc đề xuất

### Backend

```text
backend/
  StudentManagement.Api/
    Controllers/
    Data/
    Entities/
    DTOs/
    Repositories/          (chỉ dùng nếu thực sự cần)
    Services/
    Authorization/
    Middleware/
    Migrations/            (chỉ khi phù hợp với schema hiện có)
```

- Controller mỏng, không chứa truy vấn nghiệp vụ phức tạp.
- Service chịu trách nhiệm nghiệp vụ, scope theo `UserId`/role từ claims.
- DTO không expose password hash, internal path hoặc field không cần thiết.
- Dùng `AsNoTracking()` cho query read-only; dùng async EF Core.
- Dùng validation cho request body và giới hạn loại/kích thước file.
- Trả status code nhất quán: `200/201`, `204`, `400`, `401`, `403`, `404`, `409`, `413`.
- Bật Swagger ở Development, có JWT Bearer scheme.

### Frontend

```text
frontend/src/app/
  core/          auth service, interceptor, guards, role helpers, error handling
  shared/        layout, loading, confirmation, reusable UI
  features/
    auth/
    courses/
    learning/
    assignments/
```

- `AuthService` lưu token/user session, xử lý logout và token hết hạn.
- HTTP interceptor gắn `Authorization: Bearer <token>` và xử lý 401.
- Guard cho authenticated route và role route; vẫn phải dựa vào backend.
- Observable phải được unsubscribe hoặc dùng pattern phù hợp Angular 12.
- Mỗi trạng thái loading/empty/error/success cần hiển thị được cho người dùng.
- UI responsive mức cơ bản, tiếng Việt, không cần sao chép giao diện LMS cụ thể.

## 5. Hợp đồng API MVP

Prefix đề xuất: `/api`. Tên có thể thay đổi nếu có lý do tương thích schema, nhưng phải cập nhật Swagger và `docs/API_CONTRACT.md`.

### Auth

| Method | Route | Quyền | Mục đích |
|---|---|---|---|
| `POST` | `/api/auth/login` | Public | Đăng nhập; trả token, user id, display name, roles, expiresAt. |
| `GET` | `/api/auth/me` | Authenticated | Lấy session hiện tại. |
| `POST` | `/api/auth/logout` | Authenticated | MVP có thể logout ở client; endpoint tùy chọn nếu không dùng refresh token. |

### Courses/content

| Method | Route | Quyền | Mục đích |
|---|---|---|---|
| `GET` | `/api/courses` | Authenticated | Student: khóa đã enroll; Teacher: khóa được giao; Admin: tất cả. |
| `POST` | `/api/courses` | Teacher/Admin | Tạo khóa học; Teacher tự là owner nếu không có quyền assign. |
| `GET` | `/api/courses/{courseId}` | Scoped | Chi tiết khóa học và thông tin giảng viên. |
| `PUT` | `/api/courses/{courseId}` | Owner/Admin | Sửa khóa học. |
| `DELETE` | `/api/courses/{courseId}` | Owner/Admin | Xóa mềm hoặc xóa có kiểm tra ràng buộc. |
| `GET` | `/api/courses/{courseId}/chapters` | Scoped | Danh sách chương và bài theo order. |
| `POST/PUT/DELETE` | `/api/courses/{courseId}/chapters...` | Owner/Admin | Quản lý chương/bài. |
| `GET` | `/api/lessons/{lessonId}` | Scoped | Chi tiết bài học, video và tài liệu. |
| `POST` | `/api/lessons/{lessonId}/complete` | Student | Đánh dấu hoàn thành idempotent. |
| `POST` | `/api/lessons/{lessonId}/resources` | Owner/Admin | Upload tài liệu hợp lệ. |
| `GET` | `/api/resources/{resourceId}/download` | Scoped | Download tài liệu, kiểm tra quyền trước khi trả file. |

### Assignments/submissions

| Method | Route | Quyền | Mục đích |
|---|---|---|---|
| `GET` | `/api/courses/{courseId}/assignments` | Scoped | Danh sách bài tập. |
| `POST` | `/api/courses/{courseId}/assignments` | Owner/Admin | Tạo bài tập và hạn nộp. |
| `PUT/DELETE` | `/api/assignments/{assignmentId}` | Owner/Admin | Sửa/xóa bài tập. |
| `GET` | `/api/assignments/{assignmentId}` | Scoped | Chi tiết và trạng thái của user hiện tại. |
| `POST` | `/api/assignments/{assignmentId}/submissions` | Student | Nộp text, file hoặc cả hai; từ chối file quá giới hạn. |
| `GET` | `/api/assignments/{assignmentId}/submissions/me` | Student | Bài nộp của sinh viên hiện tại. |
| `GET` | `/api/assignments/{assignmentId}/submissions` | Owner/Admin | Danh sách bài nộp để chấm. |
| `PUT` | `/api/submissions/{submissionId}/grade` | Owner/Admin | Chấm điểm và nhận xét. |

## 6. Kế hoạch implementation đúng 3 phase

### Phase 1 - Nền tảng, đăng nhập, phân quyền và đọc khóa học

**Backend**

- Tạo solution/API .NET 5, cấu hình EF Core 5 SQL Server, CORS, Swagger.
- Tạo/mapping DbContext theo database thực tế; ghi `DB_SCHEMA_NOTES.md`.
- Implement JWT login, `me`, password hash an toàn, claims role.
- Implement authorization cho `STUDENT`, `TEACHER`, `ADMIN`.
- Implement `GET /courses`, `GET /courses/{id}` với query scope đúng vai trò.
- Tạo seed chỉ khi database chưa có dữ liệu cần thiết và seed an toàn, idempotent; không ghi đè tài khoản hiện có.

**Frontend**

- Khởi tạo Angular 12 và layout app.
- Login/logout, session restore, interceptor, auth guard, role guard.
- Trang danh sách khóa học và chi tiết khóa học.
- Menu hiển thị theo role; 401/403/404/error có thông báo rõ.

**Definition of Done**

- Có thể login bằng dữ liệu database local.
- Student chỉ thấy/đọc khóa đã enroll.
- Teacher chỉ thấy/đọc khóa được giao.
- Admin xem được toàn bộ khóa.
- Refresh trang không làm mất session khi token còn hạn.
- `dotnet build` và `ng build` pass.

### Phase 2 - Chương, bài học, tài liệu và tiến độ học

**Backend**

- CRUD chương/bài với order và scope teacher/admin.
- API chi tiết bài học gồm video URL/path, tài liệu, trạng thái progress.
- Upload/download PDF/tài liệu với giới hạn extension, MIME, size và tên file an toàn.
- Student đánh dấu hoàn thành idempotent; teacher xem cấu trúc nội dung.

**Frontend**

- Course learning page: sidebar chương/bài, lesson detail, video player/embed, resource list.
- Download tài liệu bằng token đúng cách; không expose đường dẫn vật lý.
- Nút đánh dấu hoàn thành, hiển thị tiến độ và trạng thái loading/error.
- Teacher UI quản lý chương, bài, video và tài liệu.

**Definition of Done**

- Nội dung hiển thị đúng thứ tự.
- Student không thể truy cập lesson/resource thuộc khóa ngoài scope.
- Upload hợp lệ thành công, file không hợp lệ bị từ chối.
- Progress được lưu và hiển thị sau khi reload.

### Phase 3 - Bài tập, nộp bài, trễ hạn và chấm điểm

**Backend**

- CRUD assignment cho teacher sở hữu khóa/admin.
- Student xem assignment của khóa mình học.
- Nộp text/file, lưu thời điểm server, xác định đúng `on-time/late`.
- Teacher xem submissions của khóa mình, grade và feedback.
- Student chỉ xem submission/điểm của chính mình.

**Frontend**

- Assignment list/detail, due date, countdown/status phù hợp.
- Form nộp text/file, trạng thái đã nộp/trễ hạn.
- Teacher grading screen có danh sách, chi tiết, score validation và feedback.
- Student xem điểm/nhận xét sau khi được chấm.

**Definition of Done**

- Student không thể nộp vào assignment ngoài khóa đã enroll.
- Teacher không thể xem/chấm submission ngoài khóa được giao.
- Điểm bị từ chối nếu âm hoặc lớn hơn max score.
- Submission sau due time hiển thị `LATE` dù client chỉnh đồng hồ.
- Tất cả 3 vai trò pass regression của phase 1 và 2.

## 7. Quy ước bàn giao giữa các phase

Sau mỗi phase, agent triển khai phải gửi:

1. Danh sách file đã tạo/sửa.
2. Tóm tắt chức năng đã hoàn thành và phần chưa làm.
3. Schema/API thay đổi, cập nhật `DB_SCHEMA_NOTES.md` và `API_CONTRACT.md`.
4. Lệnh chạy backend/frontend.
5. Kết quả `dotnet build`, `ng build`, test tự động nếu có.
6. Tài khoản/test data đã dùng, không gửi mật khẩu thật.
7. Known issues và hướng dẫn để agent kiểm thử tái hiện.

Không chuyển sang phase tiếp theo nếu phase hiện tại không build được hoặc có lỗi authorization nghiêm trọng.

## 8. Chất lượng và an toàn tối thiểu

- Không hard-code JWT secret dùng cho production; local có thể dùng user secret hoặc config bị gitignore.
- Password phải hash bằng thư viện phù hợp, không lưu plaintext.
- Validate mọi id và request body ở backend.
- Dùng parameterized EF queries, không nối chuỗi SQL từ input.
- Tên file upload phải được generate/normalize; kiểm tra content type và kích thước.
- Không commit `node_modules`, `bin`, `obj`, file upload runtime, secret.
- Có migration/script backup plan nếu cần thay đổi database.
- Tối thiểu phải có unit/integration test cho login, scope Student/Teacher, due date và grade validation.
