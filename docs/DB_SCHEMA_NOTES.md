# Database schema notes

Tài liệu này được agent triển khai cập nhật sau kết quả khảo sát read-only database thực tế.

## Connection

- Database: `StudentManagementv2Db`
- SQL Server instance: `localhost` (hỗ trợ `.` hoặc `(local)`)
- Authentication mode: Windows Authentication (Integrated Security = True; TrustServerCertificate = True)
- Kết nối thành công: Có (Kiểm tra bằng SqlClient thành công vào 2026-10-22)
- Ngày khảo sát: 2026-10-22

## Hiện trạng schema

Kết quả kiểm tra read-only: Database `StudentManagementv2Db` đã tồn tại trên SQL Server instance `localhost`, nhưng hiện tại chưa có bảng người dùng nào (`COUNT = 0`).

| Table | Purpose | Primary key | Relevant columns | Existing relationships | Mapping decision |
|---|---|---|---|---|---|
| `Users` | Lưu tài khoản, mật khẩu băm, vai trò | `Id` (INT IDENTITY) | `Username`, `Email`, `PasswordHash`, `FullName`, `Role`, `IsActive`, `CreatedAt` | 1-N với `Courses` (Teacher), 1-N với `Enrollments` (Student) | Tạo mới bảng với script idempotent / EF Core |
| `Courses` | Thông tin khóa học | `Id` (INT IDENTITY) | `Code`, `Title`, `Description`, `TeacherId`, `Status`, `CreatedAt`, `UpdatedAt` | FK `TeacherId` -> `Users(Id)` | Tạo mới bảng với script idempotent / EF Core |
| `Enrollments` | Ghi danh sinh viên vào khóa học | `Id` (INT IDENTITY) | `CourseId`, `StudentId`, `Status`, `EnrolledAt` | FK `CourseId` -> `Courses(Id)`, FK `StudentId` -> `Users(Id)` | Tạo mới bảng với script idempotent / EF Core |
| `CourseChapters` | Quản lý chương của khóa học (Phase 2) | `Id` (INT IDENTITY) | `CourseId`, `Title`, `Description`, `OrderIndex`, `CreatedAt` | FK `CourseId` -> `Courses(Id)` | Sẵn sàng tạo trong Phase 2 / initial schema |
| `Lessons` | Quản lý bài học (Phase 2) | `Id` (INT IDENTITY) | `ChapterId`, `Title`, `Description`, `OrderIndex`, `VideoUrl`, `IsPublished`, `CreatedAt` | FK `ChapterId` -> `CourseChapters(Id)` | Sẵn sàng tạo trong Phase 2 / initial schema |
| `LessonResources` | Quản lý tài liệu bài học (Phase 2) | `Id` (INT IDENTITY) | `LessonId`, `FileName`, `FilePath`, `ContentType`, `FileSize`, `CreatedAt` | FK `LessonId` -> `Lessons(Id)` | Sẵn sàng tạo trong Phase 2 / initial schema |
| `LessonProgresses` | Tiến độ học của sinh viên (Phase 2) | `Id` (INT IDENTITY) | `LessonId`, `StudentId`, `IsCompleted`, `CompletedAt` | FK `LessonId` -> `Lessons(Id)`, FK `StudentId` -> `Users(Id)` | Sẵn sàng tạo trong Phase 2 / initial schema |
| `Assignments` | Quản lý bài tập (Phase 3) | `Id` (INT IDENTITY) | `CourseId`, `Title`, `Description`, `DueDate`, `MaxScore`, `IsActive`, `CreatedAt` | FK `CourseId` -> `Courses(Id)` | Sẵn sàng tạo trong Phase 3 / initial schema |
| `Submissions` | Nộp bài và chấm điểm (Phase 3) | `Id` (INT IDENTITY) | `AssignmentId`, `StudentId`, `TextContent`, `FileName`, `FilePath`, `SubmittedAt`, `Status`, `Score`, `Feedback`, `GradedAt`, `GradedById` | FK `AssignmentId` -> `Assignments(Id)`, FK `StudentId` -> `Users(Id)` | Sẵn sàng tạo trong Phase 3 / initial schema |

## Dữ liệu khởi tạo kiểm thử (Seed an toàn, Idempotent)

Vì database chưa có tài khoản nào, áp dụng cơ chế idempotent seed: chỉ chèn khi bảng chưa có dữ liệu hoặc theo ID/Username cụ thể để phục vụ QA Acceptance Checklist:
- 1 Admin: `admin` / `Admin@123` (`ADMIN`)
- 2 Teachers: 
  - `teacher1` / `Teacher@123` (`TEACHER`) - Phân công Course 1, Course 3
  - `teacher2` / `Teacher@123` (`TEACHER`) - Phân công Course 2
- 2 Students:
  - `student1` / `Student@123` (`STUDENT`) - Enroll Course 1, Course 3
  - `student2` / `Student@123` (`STUDENT`) - Enroll Course 2, Course 3

## Quyết định an toàn dữ liệu

- Không drop database/table.
- Không xóa hoặc overwrite dữ liệu hiện có nếu sau này có dữ liệu thực.
- Kiểm tra `IF NOT EXISTS` cho tất cả table/index tạo mới.
- Password được hash an toàn (BCrypt / ASP.NET Core PasswordHasher), không lưu plaintext.
