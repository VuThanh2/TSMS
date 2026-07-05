# Technologies Needed

## 1. Frontend

### ReactJS + TypeScript
- Framework chính cho toàn bộ giao diện. Dùng React Hook (`useState`, `useEffect`, `useContext`). Không dùng Class Component.

### Ant Design (antd)
Thư viện UI component chính:
- `Table` + `Pagination` → mọi màn hình danh sách dạng Grid
- `Form` + `Input` + `Select` → mọi form tạo/chỉnh sửa
- `Modal` + `Popconfirm` → xác nhận hành động nguy hiểm
- `Upload` → import CSV
- `Tag`/`Badge` → hiển thị trạng thái Course (Upcoming/Active/Completed)

### Axios
HTTP client giao tiếp Backend REST API cho mọi Use Case có gọi API. Cấu hình instance với `baseURL` + interceptor tự gắn JWT vào header `Authorization: Bearer`.

### ECharts
Vẽ biểu đồ thống kê:
- Bar Chart — điểm trung bình theo từng khóa học (Course Statistics)
- Pie Chart — phân bố điểm sinh viên theo nhóm (Score Distribution)
- Data đến từ Projection Tables của Reporting Context (read-side), không query trực tiếp bảng gốc.

### Timeline Library (tự chọn)
Hiển thị thời khóa biểu dạng lịch/timeline (My Schedule). Có thể dùng `react-big-calendar`, `@fullcalendar/react`, hoặc tương đương — không có yêu cầu cụ thể về thư viện.

### @microsoft/signalr (JS Client)
Client-side WebSocket nhận sự kiện realtime từ SignalR Hub — chỉ dùng cho tự động làm mới điểm của Student khi Lecturer cập nhật điểm. Chỉ kết nối Hub khi Student đang ở màn hình xem điểm, không áp dụng toàn bộ ứng dụng.

## 2. Backend

### ASP.NET Core 8 Web API
Framework chính cho toàn bộ Backend. Expose REST API + WebSocket endpoint cho SignalR. Kiến trúc Clean Architecture + Modular Monolith, 4 Bounded Context: Identity, Course, Enrollment, Reporting.

### ASP.NET Core Identity
Quản lý xác thực và tài khoản: đăng nhập, đăng xuất, đặt lại mật khẩu (Password Policy), tạo user đơn lẻ/CSV, deactivate tài khoản (hủy session ngay lập tức). Dùng `PasswordHasher`, `UserManager`, `SignInManager` có sẵn — không tự xây lại cơ chế hash password.

### JWT Bearer Authentication
Cơ chế xác thực stateless. Mọi endpoint yêu cầu đăng nhập dùng header `Authorization: Bearer`. Kết hợp Identity để phát hành token sau khi login thành công.

### Role-Based Authorization (RBAC)
Phân quyền theo Admin/Lecturer/Student — mọi Controller/Endpoint bảo vệ bằng `[Authorize(Roles = "...")]`. Role gán khi tạo tài khoản, không đổi được sau đó.

### MediatR (CQRS)
- **Command**: mọi thao tác ghi (tạo, cập nhật, xóa)
- **Query**: mọi thao tác đọc
- **INotification**: phát Domain Event nội bộ (vd `GradeUpdatedEvent` → Outbox → Reporting)
- Phân biệt rõ `IRequest<T>` và `INotification` — không nhầm lẫn 2 loại này.

### FluentValidation
Validate dữ liệu đầu vào tại Application Layer cho mọi Command có dữ liệu từ user. Đặt validator ở Application Layer, không đặt trong Domain hay Controller.

### SignalR (Server-side Hub)
Push sự kiện realtime từ server xuống client — sau khi Lecturer lưu điểm, Hub push thông báo tới đúng Student đang online (target theo `studentId`, không broadcast toàn bộ). Chỉ dùng cho tính năng này, không dùng cho gì khác.

### Hangfire
Background Job Scheduler:
- Job định kỳ tự động cập nhật trạng thái Course (Upcoming → Active → Completed)
- Outbox Worker: poll bảng Outbox, dispatch Domain Event pending tới handler cập nhật Projection Tables
- 2 loại job độc lập nhau, cần phân biệt rõ khi cấu hình. Hangfire cần schema/database riêng cho job queue.

### Interface Contracts (Cross-BC)
Cơ chế giao tiếp giữa các Bounded Context không chia sẻ database hay gọi trực tiếp — vd Enrollment/Reporting đọc dữ liệu Course qua interface thay vì JOIN trực tiếp bảng Course. Mô phỏng inter-service call của Microservice nhưng chạy in-process trong Modular Monolith.

## 3. Infrastructure

### Entity Framework Core (Code First)
ORM ánh xạ Domain Entity sang bảng DB + quản lý migration. Mỗi Bounded Context có `DbContext` độc lập — KHÔNG dùng chung 1 `DbContext` cho toàn hệ thống. Cấu hình qua `IEntityTypeConfiguration<T>`, không dùng Data Annotation trực tiếp trên Entity.

### SQL Server
Lưu toàn bộ dữ liệu — Users, Courses, ClassSessions, Enrollments, Grades, Projection Tables, Hangfire job queue. Các BC chia sẻ 1 SQL Server instance nhưng có schema riêng biệt.

### Outbox Pattern (MediatR + Hangfire)
Đảm bảo Domain Event được publish đáng tin cậy (at-least-once) không mất dữ liệu khi lỗi. Sau khi lưu điểm/điểm danh, event được ghi vào bảng Outbox; Hangfire Worker định kỳ đọc và dispatch → cập nhật Projection Tables Reporting. Eventual consistency được chấp nhận cho Reporting — dữ liệu báo cáo có thể chậm vài giây so với thực tế.

### NotificationLog
Bảng tracking trạng thái gửi email nhắc ca học, dùng để deduplication (mỗi người chỉ nhận 1 email/ca học). Thuộc Infrastructure layer, không gắn với BC nào, không mang domain meaning. Cấu trúc: `UserId`, `ClassSessionId`, `SentAt`. Job đọc bảng này trước khi gửi — nếu đã có bản ghi thì bỏ qua; nếu gửi thất bại thì không ghi, job lần sau thử lại.