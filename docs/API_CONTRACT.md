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
- **Response `401 Unauthorized`** (Khi sai username hoặc sai mật khẩu - không tiết lộ user có tồn tại hay không):
  ```json
  {
    "message": "Tên đăng nhập hoặc mật khẩu không chính xác"
  }
  ```
- **Response `400 Bad Request`** (Khi thiếu trường bắt buộc).

### 1.2 Lấy thông tin phiên hiện tại

- **Route**: `GET /api/auth/me`
- **Quyền**: Authenticated (`STUDENT`, `TEACHER`, `ADMIN`)
- **Headers**: `Authorization: Bearer <token>`
- **Response `200 OK`**:
  ```json
  {
    "userId": 4,
    "username": "student1",
    "fullName": "Học sinh Lê Văn C",
    "email": "student1@lms.local",
    "role": "STUDENT"
  }
  ```
- **Response `401 Unauthorized`**: Không có token hoặc token hết hạn.

### 1.3 Đăng xuất hệ thống

- **Route**: `POST /api/auth/logout`
- **Quyền**: Authenticated
- **Response `200 OK`**:
  ```json
  {
    "message": "Đăng xuất thành công"
  }
  ```

---

## 2. Courses (`/api/courses`)

### 2.1 Danh sách khóa học theo phân quyền (Scoped List)

- **Route**: `GET /api/courses`
- **Quyền**: Authenticated (`STUDENT`, `TEACHER`, `ADMIN`)
- **Quy tắc phân quyền (Scope Invariant)**:
  - `STUDENT`: Chỉ trả về các khóa học mà sinh viên đã được ghi danh (`Enrollments`).
  - `TEACHER`: Chỉ trả về các khóa học được phân công giảng dạy (`teacherId == currentUserId`).
  - `ADMIN`: Trả về toàn bộ khóa học trong hệ thống.
- **Response `200 OK`**:
  ```json
  [
    {
      "id": 1,
      "code": "CS201",
      "title": "Kiến trúc Web API và Bảo mật Hệ thống",
      "description": "Khóa học nâng cao về RESTful API...",
      "teacherId": 2,
      "teacherName": "Thầy Nguyễn Văn A",
      "status": "Active",
      "createdAt": "2026-10-22T08:01:13.456Z",
      "updatedAt": null,
      "isEnrolled": true,
      "enrollmentCount": 2
    }
  ]
  ```

### 2.2 Chi tiết khóa học theo phân quyền (Scoped Detail)

- **Route**: `GET /api/courses/{id}`
- **Quyền**: Authenticated (`STUDENT`, `TEACHER`, `ADMIN`)
- **Quy tắc kiểm tra quyền sở hữu/ghi danh (Ownership/Enrollment Invariant)**:
  - Sinh viên chưa ghi danh khóa học này -> Trả về `403 Forbidden` (`{"message": "Bạn không có quyền truy cập khóa học này"}`).
  - Giảng viên không được phân công khóa học này -> Trả về `403 Forbidden` (`{"message": "Bạn không có quyền quản lý khóa học này"}`).
  - Khóa học không tồn tại -> Trả về `404 Not Found`.
- **Response `200 OK`**:
  ```json
  {
    "id": 1,
    "code": "CS201",
    "title": "Kiến trúc Web API và Bảo mật Hệ thống",
    "description": "Khóa học nâng cao về RESTful API...",
    "teacherId": 2,
    "teacherName": "Thầy Nguyễn Văn A",
    "status": "Active",
    "createdAt": "2026-10-22T08:01:13.456Z",
    "updatedAt": null,
    "isEnrolled": true,
    "enrollmentCount": 2
  }
  ```

### 2.3 Tạo khóa học mới

- **Route**: `POST /api/courses`
- **Quyền**: `TEACHER`, `ADMIN`
- **Request Body**:
  ```json
  {
    "code": "CS301",
    "title": "Lập trình Di động Flutter",
    "description": "Khóa học xây dựng ứng dụng di động đa nền tảng"
  }
  ```
- **Quy tắc**:
  - `TEACHER`: Tự động gán `teacherId = currentUserId`.
  - `ADMIN`: Có thể truyền `teacherId` để chỉ định giảng viên.
  - Trùng mã khóa học (`code`) -> Trả về `400 Bad Request` (`{"message": "Mã khóa học đã tồn tại trong hệ thống"}`).
- **Response `201 Created`**: Trả về dữ liệu `CourseDto` và header `Location`.

### 2.4 Cập nhật khóa học

- **Route**: `PUT /api/courses/{id}`
- **Quyền**: `TEACHER` (chỉ khóa học của mình), `ADMIN`
- **Request Body**:
  ```json
  {
    "title": "Kiến trúc Web API và Microservices",
    "description": "Cập nhật đề cương môn học mới",
    "status": "Active"
  }
  ```
- **Quy tắc**:
  - `TEACHER` sửa khóa học của giảng viên khác -> Trả về `403 Forbidden`.
  - Không tìm thấy khóa học -> Trả về `404 Not Found`.
- **Response `200 OK`**: Trả về `CourseDto` đã cập nhật.

### 2.5 Xóa khóa học

- **Route**: `DELETE /api/courses/{id}`
- **Quyền**: `TEACHER` (chỉ khóa học của mình), `ADMIN`
- **Quy tắc**:
  - `TEACHER` xóa khóa học của giảng viên khác -> Trả về `403 Forbidden`.
  - Không tìm thấy khóa học -> Trả về `404 Not Found`.
- **Response `204 No Content`**

---

## 3. Bảng tổng hợp Endpoint Phase 1

| Method | Route | Auth / Role | Request Body | Response | Status Codes |
|---|---|---|---|---|---|
| `POST` | `/api/auth/login` | Public | `LoginRequest` | `LoginResponse` | 200, 400, 401 |
| `GET` | `/api/auth/me` | Authenticated | None | `CurrentUserResponse` | 200, 401, 404 |
| `POST` | `/api/auth/logout` | Authenticated | None | `{ message }` | 200, 401 |
| `GET` | `/api/courses` | Authenticated | None | `List<CourseDto>` | 200, 401 |
| `GET` | `/api/courses/{id}` | Scoped | None | `CourseDto` | 200, 401, 403, 404 |
| `POST` | `/api/courses` | `TEACHER`, `ADMIN` | `CreateCourseRequest` | `CourseDto` | 201, 400, 401, 403 |
| `PUT` | `/api/courses/{id}` | Owner `TEACHER`, `ADMIN` | `UpdateCourseRequest` | `CourseDto` | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/courses/{id}` | Owner `TEACHER`, `ADMIN` | None | None | 204, 401, 403, 404 |
