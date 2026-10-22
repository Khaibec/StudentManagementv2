# StudentManagementv2 - Tài liệu triển khai MVP

Hệ thống LMS quản lý học sinh và học tập xây dựng bằng **ASP.NET Core Web API .NET 5**, **Angular 12**, và **SQL Server** (`StudentManagementv2Db`).

## Hướng dẫn chạy hệ thống

### 1. Cơ sở dữ liệu

- SQL Server local instance: `localhost` (Windows Integrated Security).
- Database: `StudentManagementv2Db`.
- Hệ thống tự động tạo bảng (idempotent qua `init_schema.sql` và `DbInitializer`) và seed tài khoản ban đầu nếu chưa có dữ liệu.

### 2. Backend Web API (.NET 5)

```powershell
cd backend/StudentManagement.Api
dotnet run
```

- API Base URL: `http://localhost:5000` (HTTPS: `https://localhost:5001`)
- Swagger UI tài liệu endpoint tương tác: `http://localhost:5000/swagger`

### 3. Frontend Angular (Angular 12)

```powershell
cd frontend
npm start
```

- Web UI URL: `http://localhost:4200`

### 4. Kiểm thử tự động (Unit & Integration Tests)

```powershell
cd backend
dotnet test
```

- 8/8 tests pass (Kiểm tra xác thực, thông báo lỗi an toàn, phân quyền danh sách và bảo vệ truy cập theo role/scope).

---

## Danh sách tài khoản kiểm thử đã khởi tạo

| Role | Username | Mật khẩu | Họ và tên | Phạm vi phân công / Ghi danh |
|---|---|---|---|---|
| `ADMIN` | `admin` | `Admin@123` | Quản Trị Viên Hệ Thống | Toàn bộ khóa học (CS101, CS102, CS201) |
| `TEACHER` | `teacher1` | `Teacher@123` | Thầy Nguyễn Văn A | Phân công phụ trách `CS101`, `CS201` |
| `TEACHER` | `teacher2` | `Teacher@123` | Cô Trần Thị B | Phân công phụ trách `CS102` |
| `STUDENT` | `student1` | `Student@123` | Học sinh Lê Văn C | Đã ghi danh `CS101`, `CS201` (Không thấy `CS102`) |
| `STUDENT` | `student2` | `Student@123` | Học sinh Phạm Thị D | Đã ghi danh `CS102`, `CS201` (Không thấy `CS101`) |
