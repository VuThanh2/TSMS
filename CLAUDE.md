# TSMS — Teaching Schedule Management System

Hệ thống Quản lý Lịch Dạy học, dự án thực tập tại IDTEK. 3 role: **Admin**, **Lecturer**, **Student**.
Trước khi code bất kỳ phần nào, đọc file này trước — đây là nguồn convention duy nhất, không đoán theo thói quen mặc định của framework.

## Nguyên tắc bắt buộc cho MỌI đoạn code (Backend lẫn Frontend)

Luôn tuân thủ: convention riêng của project (bên dưới), best practice của ngôn ngữ/framework đang dùng,
Clean Code, Clean Architecture, DDD, OOP, và Design Pattern phù hợp với bài toán — không chỉ viết code chạy được.
Luôn giải thích ngắn gọn lý do kiến trúc khi đưa ra quyết định, không chỉ đưa code.

---

## 1. Backend

### Kiến trúc
.NET **Modular Monolith** + Clean Architecture + DDD + CQRS/MediatR + EF Core + SQL Server (1 database, 4 schema),
Hangfire (background job), SignalR (real-time), Outbox pattern (eventual consistency giữa các module).

### 4 Bounded Context
| BC | Schema | Trách nhiệm |
|---|---|---|
| `Identity` | `identity` | Auth, CRUD user (ASP.NET Core Identity) |
| `CourseManagement` | `course` | Course lifecycle, ClassSession, WeeklySlot, Hangfire status transition |
| `EnrollmentManagement` | `enrollment` | Enrollment, chọn WeeklySlot, chấm điểm, điểm danh, truy vấn lịch |
| `Reporting` | `reporting` | Read-only projection, cập nhật qua Domain Event, KHÔNG JOIN cross-BC lúc query |

### Convention bắt buộc
- Brace mở cùng dòng: `public void Method() {`
- Comment: chỉ dùng `//` hoặc `///`, KHÔNG dùng XML `<summary>` block
- Comment nội dung bằng **tiếng Việt**, identifier bằng **tiếng Anh**
- **DTO**: `InputDto` + `OutputDto` cho cùng 1 use case, gộp trong 1 file `XxxDto.cs` (KHÔNG đặt tên số nhiều `XxxDtos.cs`). Use case chỉ có Query (không có Input) vẫn đặt tên file `XxxDto.cs`, record bên trong tên `XxxOutputDto`
- **3 file / use case**: `XxxCommand.cs` (record + handler chung file, filename KHÔNG có hậu tố "Handler"), `XxxValidator.cs`, `XxxDto.cs`
- **Mapper**: static class theo từng Entity, đặt ở `Common/Mappers/` (không tạo mapper riêng theo từng use case)
- **Cross-BC interface**: định nghĩa ở Application layer của BC **tiêu thụ** (consuming), implement ở Infrastructure layer của BC **sở hữu** (owning); đăng ký DI theo hướng forward, 1 instance nhiều interface
- Domain tự validate invariant của chính nó; Application layer chịu trách nhiệm precondition cross-aggregate (schedule conflict, lecturer ownership...)
- EF Core: dùng lambda `HasMany(e => e.Property)`, không dùng string backing field; tránh gọi `Update()` trên aggregate đã tracked sau khi mutate child entity — dùng repository method tường minh (`AddXxx`)

---

## 2. Frontend

### Stack
Vite + React + TypeScript, **pnpm**, TanStack Query, axios, react-router-dom, `@microsoft/signalr`, Tailwind CSS v4 (qua `@tailwindcss/vite`, KHÔNG dùng `tailwind.config.js`/`postcss.config.js` kiểu v3).

### UI Library bắt buộc (theo docs/TechnologiesNeeded.md — KHÔNG tự chọn thư viện khác)
- **Ant Design (antd)**: UI component chính cho toàn bộ app.
  - `Table` + built-in pagination → mọi màn hình Grid (User List, Course Grid, Course Student List...)
  - `Form` + `Input` + `Select` → mọi form tạo/sửa (Create User, Create Course, Add WeeklySlot, Enroll Course...)
  - `Modal` + `Popconfirm` → xác nhận hành động nguy hiểm (Activate/Deactivate, Delete WeeklySlot)
  - `Upload` → Import User CSV
  - `Tag`/`Badge` → hiển thị trạng thái Course (Upcoming/Active/Completed), buổi học đã hủy
- **ECharts** (qua `echarts-for-react` hoặc gọi `echarts` trực tiếp): Bar Chart (Course Statistics) + Pie Chart (Score Distribution). Data lấy từ Reporting ReadModel qua API, KHÔNG tự tính toán lại ở FE.
- **Timeline library cho Schedule (UC-26)**: tự chọn `react-big-calendar` hoặc `@fullcalendar/react` — không có yêu cầu bắt buộc thư viện cụ thể, miễn hiển thị được dạng lịch/timeline theo tuần/tháng.

### Cấu trúc thư mục (module-based, map 1:1 với Bounded Context)
```
src/
  modules/
    identity/{login, reset-password, user-management}
    course-management/{course-grid, course-detail, create-course, shared}
    enrollment-management/{enrollment, grading, attendance, schedule}
    reporting/{course-statistics, course-report, personal-summary}
  shared/{components, hooks, lib, types}
  app/{routes, layouts}
```

### Convention bắt buộc
- **3 file / feature**: `XxxPage.tsx`, `useXxx.ts` (hook chứa logic + TanStack Query), `xxx.api.ts` (hàm gọi axios), `xxx.types.ts` (nếu cần)
- **Rule of Three cho `shared/`**: chỉ đưa lên `shared/` (global) khi ≥ 2 **module khác nhau** cùng dùng. Nếu chỉ dùng trong nội bộ 1 module → đặt ở `modules/<module>/shared/`, không đẩy lên global
- **API envelope**: mọi response map qua `ApiResult<T>` / `PagedResult<T>` (định nghĩa ở `shared/types/api.types.ts`), khớp với `Result<T>` bên Backend — không tự ý parse response tự do trong từng `xxx.api.ts`
- **Data-fetching**: dùng TanStack Query (`useQuery`/`useMutation`), KHÔNG tự viết `useEffect + useState` để fetch data
- **HTTP client**: luôn qua instance `shared/lib/axios.ts` (đã có interceptor JWT + xử lý 401), không tạo axios instance riêng trong module
- Path alias: `@/` trỏ vào `src/`

---

## 3. Tài liệu tham chiếu trong repo

- `docs/screen-inventory.md` — danh sách toàn bộ màn hình, role được xem, UC tương ứng
- `docs/api-contract-mapping.md` — mapping từng màn hình ↔ endpoint thật, field cần, request body, lỗi cần xử lý
- `docs/auth-flow.md` — quyết định JWT storage, route guard, SignalR re-auth — bắt buộc đọc trước khi đụng vào `auth-context.tsx`/`ProtectedRoute.tsx`/`signalr.ts`
- `docs/technologies-needed.md` — UI library bắt buộc (Ant Design, ECharts, Timeline library) + mapping Use Case → công nghệ. Lưu ý: số UC trong file này là phiên bản CŨ hơn Screen Inventory/API Mapping (lệch vài số), chỉ tin phần công nghệ, không tin phần số UC ở đây

Khi code 1 màn hình cụ thể, **luôn đối chiếu các file trên trước** — đây là nguồn đúng nhất (đã verify trực tiếp từ Controller/Query trong code hoặc quyết định kiến trúc đã chốt), KHÔNG suy đoán field, endpoint, hay tự chọn thư viện khác.

## 4. Việc KHÔNG được làm

- Không tự ý đổi kiến trúc single-DB-multi-schema sang multi-DB
- Không JOIN cross-BC trong Reporting queries — chỉ dùng ReadModel đã denormalize sẵn qua Domain Event
- Không import trực tiếp giữa 2 BC ở compile-time — luôn qua interface cross-BC