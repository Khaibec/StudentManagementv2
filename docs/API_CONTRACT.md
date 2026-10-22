# API Contract - Living Document

Tài liệu này ghi lại route thực tế, request/response mẫu, status code và quyền truy cập cho hệ thống LMS `StudentManagementv2`. Swagger UI tại `http://localhost:5000/swagger` phản ánh đúng các endpoint này.

## Quy tắc chung

- **Base URL**: `http://localhost:5000/api`
- **Thời gian**: ISO 8601 UTC (`YYYY-MM-DDTHH:mm:ssZ`).
- **Bảo mật**: Mọi endpoint (trừ login) yêu cầu header `Authorization: Bearer <token>`.
- Không trả password hash, JWT secret, đường dẫn vật lý hoặc trường nội bộ.

---

## 1. Authentication (`/api/auth`)

### 1.1 Đăng nhập hệ thống

- **Route**: `POST /api/auth/login`
- **Quyền**: Public
- **Request Body**:
  ```json
  {
    "username": "student1",
    "password": "Student@123"
  }
  ```
- **Response `200 OK`**:
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 4,
    "username": "student1",
    "fullName": "Học sinh Lê Văn C",
    "role": "STUDENT",
    "expiresAt": "2026-10-22T10:14:24.1234567Z"
  }
  ```
- **Response `401 Unauthorized`**: Mật khẩu hoặc tài khoản sai (thông báo an toàn, không để lộ tài khoản tồn tại).

### 1.2 Lấy thông tin phiên hiện tại

- **Route**: `GET /api/auth/me`
- **Quyền**: Authenticated (`STUDENT`, `TEACHER`, `ADMIN`)
- **Response `200 OK`**: Trả về `userId`, `username`, `fullName`, `email`, `role`.

### 1.3 Đăng xuất hệ thống

- **Route**: `POST /api/auth/logout`
- **Quyền**: Authenticated

---

## 2. Courses (`/api/courses`)

- `GET /api/courses`: Danh sách khóa học theo scope (Student: khóa học đã ghi danh; Teacher: khóa học được phân công; Admin: toàn bộ).
- `GET /api/courses/{id}`: Chi tiết khóa học theo scope (Chặn 403 nếu ngoài scope).
- `POST /api/courses`: Tạo khóa học mới (Teacher tự gán owner; Admin).
- `PUT /api/courses/{id}`: Sửa khóa học (Owner Teacher hoặc Admin).
- `DELETE /api/courses/{id}`: Xóa khóa học (Owner Teacher hoặc Admin).

---

## 3. Course Content, Chapters & Lessons (Phase 2)

### 3.1 Danh sách chương và bài học của khóa học

- **Route**: `GET /api/courses/{courseId}/chapters`
- **Quyền**: Authenticated (Scoped: Student phải ghi danh, Teacher phải phụ trách, Admin toàn quyền).
- **Sắp xếp**: Sắp xếp theo `OrderIndex ASC`.
- **Response `200 OK`**:
  ```json
  [
    {
      "id": 1,
      "courseId": 3,
      "title": "Chương 1: Tổng quan về C#",
      "description": "Nền tảng .NET 5",
      "orderIndex": 1,
      "createdAt": "2026-10-22T08:21:44Z",
      "lessons": [
        {
          "id": 2,
          "chapterId": 1,
          "title": "Bài 1.1: Cài đặt môi trường",
          "orderIndex": 1,
          "isPublished": true,
          "isCompleted": false,
          "completedAt": null,
          "resourceCount": 1
        }
      ]
    }
  ]
  ```

### 3.2 Tạo chương mới

- **Route**: `POST /api/courses/{courseId}/chapters`
- **Quyền**: Owner Teacher, Admin
- **Request Body**:
  ```json
  {
    "title": "Chương 3: Lập trình bất đồng bộ Async/Await",
    "description": "Task, Threading và TPL trong C#",
    "orderIndex": 3
  }
  ```
- **Response `201 Created`**

### 3.3 Sửa / Xóa chương

- **Route**: `PUT /api/chapters/{chapterId}` (Sửa chương)
- **Route**: `DELETE /api/chapters/{chapterId}` (Xóa chương)
- **Quyền**: Owner Teacher, Admin (Kiểm tra ownership, chặn 403 nếu không phải chủ khóa).

### 3.4 Tạo bài học mới trong chương

- **Route**: `POST /api/chapters/{chapterId}/lessons`
- **Quyền**: Owner Teacher, Admin
- **Request Body**:
  ```json
  {
    "title": "Bài 3.1: async và await",
    "description": "Cơ chế hoạt động của State Machine",
    "orderIndex": 1,
    "videoUrl": "https://www.youtube.com/watch?v=0kFh2oNnU7k",
    "isPublished": true
  }
  ```
- **Response `201 Created`**

### 3.5 Xem chi tiết bài học (Scoped)

- **Route**: `GET /api/lessons/{id}`
- **Quyền**: Authenticated (Scoped)
- **Quy tắc**: Sinh viên ngoài khóa hoặc bài chưa publish bị chặn `403 Forbidden`.
- **Response `200 OK`**:
  ```json
  {
    "id": 2,
    "chapterId": 1,
    "chapterTitle": "Chương 1: Tổng quan về C#",
    "courseId": 3,
    "courseTitle": "Lập trình C# và .NET Căn Bản",
    "title": "Bài 1.1: Giới thiệu kiến trúc .NET 5",
    "description": "Nội dung chi tiết...",
    "orderIndex": 1,
    "videoUrl": "https://www.youtube.com/watch?v=0kFh2oNnU7k",
    "isPublished": true,
    "createdAt": "2026-10-22T08:21:44Z",
    "isCompleted": true,
    "completedAt": "2026-10-22T08:27:08.860Z",
    "resources": [
      {
        "id": 1,
        "lessonId": 2,
        "fileName": "tailieu_csharp.pdf",
        "contentType": "application/pdf",
        "fileSize": 1048576,
        "createdAt": "2026-10-22T08:27:32Z"
      }
    ]
  }
  ```

### 3.6 Đánh dấu hoàn thành bài học (Idempotent)

- **Route**: `POST /api/lessons/{id}/complete`
- **Quyền**: `STUDENT` (Sinh viên đã ghi danh vào khóa học)
- **Quy tắc Idempotent**: Gọi nhiều lần không tạo bản ghi trùng lặp trong database, luôn trả về `isCompleted = true` và thời gian hoàn thành.
- **Response `200 OK`**:
  ```json
  {
    "lessonId": 2,
    "studentId": 4,
    "isCompleted": true,
    "completedAt": "2026-10-22T08:27:08.860Z"
  }
  ```

### 3.7 Tải lên tài liệu học tập

- **Route**: `POST /api/lessons/{id}/resources`
- **Quyền**: Owner Teacher, Admin
- **Request**: Multipart/form-data (`file`).
- **Giới hạn**:
  - Đuôi tệp cho phép: `.pdf`, `.docx`, `.doc`, `.pptx`, `.ppt`, `.xlsx`, `.xls`, `.txt`, `.zip`, `.png`, `.jpg`, `.jpeg`.
  - Dung lượng tối đa: 25 MB.
  - Tên tệp an toàn: Khử ký tự path traversal, lưu file vật lý bằng GUID prefix.
- **Response `201 Created`**: Trả về metadata của file.

### 3.8 Tải xuống tài liệu học tập (Scoped Download)

- **Route**: `GET /api/resources/{id}/download`
- **Quyền**: Authenticated (Scoped: Student phải ghi danh khóa học; Teacher phải phụ trách khóa; Admin).
- **Quy tắc**: Người dùng không thuộc khóa học bị chặn `403 Forbidden`. Không để lộ đường dẫn vật lý máy chủ.
- **Response `200 OK`**: File stream nhị phân kèm Content-Type và Content-Disposition chính xác.

---

## 4. Bảng tổng hợp Endpoint Phase 2

| Method | Route | Auth / Role | Request | Response | Status Codes |
|---|---|---|---|---|---|
| `GET` | `/api/courses/{courseId}/chapters` | Scoped | None | `List<ChapterDto>` | 200, 401, 403, 404 |
| `POST` | `/api/courses/{courseId}/chapters` | Owner Teacher, Admin | `CreateChapterRequest` | `ChapterDto` | 201, 400, 401, 403, 404 |
| `PUT` | `/api/chapters/{chapterId}` | Owner Teacher, Admin | `UpdateChapterRequest` | `ChapterDto` | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/chapters/{chapterId}` | Owner Teacher, Admin | None | None | 204, 401, 403, 404 |
| `POST` | `/api/chapters/{chapterId}/lessons` | Owner Teacher, Admin | `CreateLessonRequest` | `LessonDetailDto` | 201, 400, 401, 403, 404 |
| `GET` | `/api/lessons/{id}` | Scoped | None | `LessonDetailDto` | 200, 401, 403, 404 |
| `PUT` | `/api/lessons/{id}` | Owner Teacher, Admin | `UpdateLessonRequest` | `LessonDetailDto` | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/lessons/{id}` | Owner Teacher, Admin | None | None | 204, 401, 403, 404 |
| `POST` | `/api/lessons/{id}/complete` | Student (Enrolled) | None | `LessonProgressDto` | 200, 401, 403, 404 |
| `POST` | `/api/lessons/{id}/resources` | Owner Teacher, Admin | Form file | `ResourceDto` | 201, 400, 401, 403, 404 |
| `GET` | `/api/resources/{id}/download` | Scoped | None | File Stream | 200, 401, 403, 404 |
| `DELETE` | `/api/resources/{id}` | Owner Teacher, Admin | None | None | 204, 401, 403, 404 |

---

## 5. Quản lý Bài tập, Nộp bài & Chấm điểm (Phase 3)

### 5.1 Lấy danh sách bài tập theo khóa học
- **Route**: `GET /api/courses/{courseId}/assignments`
- **Quyền**: Authenticated (Scoped)
  - `STUDENT`: Chỉ xem được bài tập của các khóa đã ghi danh.
  - `TEACHER`: Chỉ xem được bài tập của khóa được phân công giảng dạy.
  - `ADMIN`: Toàn quyền.
- **Response `200 OK`**: Danh sách `AssignmentDto` (Bao gồm trạng thái đã nộp, điểm số của sinh viên hoặc số lượng bài nộp đối với giảng viên).

### 5.2 Lấy chi tiết bài tập
- **Route**: `GET /api/assignments/{id}`
- **Quyền**: Authenticated (Scoped tương tự 5.1)
- **Response `200 OK`**: `AssignmentDetailDto` kèm chi tiết bài nộp cá nhân (`mySubmission`) nếu người gọi là học sinh.

### 5.3 Tạo bài tập mới
- **Route**: `POST /api/courses/{courseId}/assignments`
- **Quyền**: Owner Teacher, Admin
- **Request Body**:
  ```json
  {
    "title": "Bài tập 1: Giải phương trình bậc 2",
    "description": "Viết chương trình C# nộp mã nguồn",
    "dueDate": "2026-10-30T17:00:00Z",
    "maxScore": 100,
    "isActive": true
  }
  ```
- **Response `201 Created`**: Metadata bài tập mới tạo.

### 5.4 Cập nhật bài tập
- **Route**: `PUT /api/assignments/{id}`
- **Quyền**: Owner Teacher, Admin
- **Response `200 OK`**: Thông tin bài tập sau cập nhật.

### 5.5 Xóa bài tập
- **Route**: `DELETE /api/assignments/{id}`
- **Quyền**: Owner Teacher, Admin
- **Response `204 NoContent`**. Tự động dọn dẹp các tệp bài nộp vật lý liên quan.

### 5.6 Nộp bài tập (Submission)
- **Route**: `POST /api/assignments/{id}/submissions`
- **Quyền**: `STUDENT` (Phải ghi danh khóa học)
- **Content-Type hỗ trợ**: `multipart/form-data` hoặc `application/json`
  - Tham số: `textContent` (văn bản bài làm), `file` (tệp đính kèm tối đa 25MB).
- **Quy tắc tính toán thời gian**:
  - `submittedAt` được ghi nhận theo **giờ UTC của máy chủ** tại thời điểm xử lý request.
  - Nếu `submittedAt <= DueDate` -> `Status = "SUBMITTED_ON_TIME"`.
  - Nếu `submittedAt > DueDate` -> `Status = "SUBMITTED_LATE"`. Client chỉnh đồng hồ không ảnh hưởng đến server.
- **Cơ chế nộp lại (Resubmission)**: Idempotent - cập nhật bản ghi duy nhất của học sinh cho bài tập đó và reset điểm số chờ chấm lại.
- **Response `200 OK`**: `SubmissionDto`.

### 5.7 Xem bài nộp của bản thân
- **Route**: `GET /api/assignments/{id}/submissions/me`
- **Quyền**: `STUDENT` (Chỉ xem bài nộp của chính mình)
- **Response `200 OK`**: `SubmissionDto` gồm nội dung, thời gian nộp, file đính kèm, điểm và nhận xét của giảng viên.

### 5.8 Giảng viên xem danh sách bài nộp của bài tập
- **Route**: `GET /api/assignments/{id}/submissions`
- **Quyền**: Owner Teacher, Admin
- **Response `200 OK`**: `List<SubmissionDto>` của toàn bộ học sinh trong lớp.

### 5.9 Chấm điểm và phản hồi bài nộp
- **Route**: `PUT /api/submissions/{id}/grade`
- **Quyền**: Owner Teacher, Admin
- **Request Body**:
  ```json
  {
    "score": 95.5,
    "feedback": "Bài làm rất tốt, cấu trúc code rõ ràng."
  }
  ```
- **Ràng buộc điểm số**: `0 <= score <= MaxScore`. Điểm âm hoặc vượt `MaxScore` bị từ chối `400 Bad Request`.
- **Response `200 OK`**: `SubmissionDto` với điểm, nhận xét, thời gian chấm và thông tin người chấm.

### 5.10 Tải tệp bài nộp
- **Route**: `GET /api/submissions/{id}/download`
- **Quyền**: Học sinh chủ bài nộp, Giáo viên phụ trách khóa, hoặc Admin.
- **Response `200 OK`**: File stream nhị phân.

---

## 6. Bảng tổng hợp Endpoint Phase 3

| Method | Route | Auth / Role | Request | Response | Status Codes |
|---|---|---|---|---|---|
| `GET` | `/api/courses/{courseId}/assignments` | Scoped | None | `List<AssignmentDto>` | 200, 401, 403, 404 |
| `POST` | `/api/courses/{courseId}/assignments` | Owner Teacher, Admin | `CreateAssignmentRequest` | `AssignmentDto` | 201, 400, 401, 403, 404 |
| `GET` | `/api/assignments/{id}` | Scoped | None | `AssignmentDetailDto` | 200, 401, 403, 404 |
| `PUT` | `/api/assignments/{id}` | Owner Teacher, Admin | `UpdateAssignmentRequest` | `AssignmentDto` | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/assignments/{id}` | Owner Teacher, Admin | None | None | 204, 401, 403, 404 |
| `POST` | `/api/assignments/{id}/submissions` | Enrolled Student | Multipart hoặc JSON | `SubmissionDto` | 200, 400, 401, 403, 404 |
| `GET` | `/api/assignments/{id}/submissions/me` | Enrolled Student | None | `SubmissionDto` | 200, 401, 403, 404 |
| `GET` | `/api/assignments/{id}/submissions` | Owner Teacher, Admin | None | `List<SubmissionDto>` | 200, 401, 403, 404 |
| `PUT` | `/api/submissions/{id}/grade` | Owner Teacher, Admin | `GradeSubmissionRequest` | `SubmissionDto` | 200, 400, 401, 403, 404 |
| `GET` | `/api/submissions/{id}/download` | Submitter, Owner Teacher, Admin | None | File Stream | 200, 401, 403, 404 |

