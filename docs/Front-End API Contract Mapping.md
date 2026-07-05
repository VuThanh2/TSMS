# Front-End API Contract Mapping

Mapping từng màn hình trong Screen Inventory với Endpoint thực tế (đối chiếu trực tiếp từ Controller/Query trong code, không chỉ từ tài liệu Endpoint). Mục tiêu: biết trước field nào có, field nào thiếu, request cần gửi gì, trước khi code UI. Tất cả endpoint (trừ Login/Forgot/Reset Password) đều yêu cầu header `Authorization: Bearer {token}`.

# 1. Authentication

| Màn hình | Endpoint | Field cần cho UI | Request Body | Ghi chú |
| --- | --- | --- | --- | --- |
| Login Screen | `POST /api/auth/login` | `accessToken` (không có `refreshToken`) | `{ email, password }` | Controller tự viết (không dùng `MapIdentityApi`) vì cần custom JWT claims: `role, fullName, isActive`. FE decode JWT để lấy `role` dùng redirect đúng trang chính, không cần gọi thêm API nào khác để biết role |
| Reset Password Screen | `POST /api/auth/reset-password` | — | `{ email, newPassword }` | ⚠️ Chỉ 1 API duy nhất, không có bước gửi `resetCode` qua email, không có endpoint `/forgotPassword` riêng — UI chỉ cần 1 form (email + mật khẩu mới), submit trực tiếp. Error: `User.NotFound`, `User.AccountIsInactive`, `User.PasswordPolicyViolation` |
| Logout | `POST /api/auth/logout` | — | — (cần Bearer token) | Không phải màn hình riêng — nút trên Navbar. Trả 204, FE tự xóa token khỏi storage sau khi gọi thành công |

# 2. User Management

| Màn hình | Endpoint | Field cần cho UI | Request Body | Ghi chú |
| --- | --- | --- | --- | --- |
| User List Screen | `GET /api/users?page=&pageSize=` | `userId, fullName, email, role, isActive`  • `totalCount` để phân trang | — | ✅ Đã hỗ trợ `?search=&role=`  |
| Create User Modal | `POST /api/users` | — | `{ fullName, email, role, password }` | Error cần xử lý: `EmailAlreadyExists`, `InvalidRole`, `PasswordPolicyViolation` |
| Import CSV Modal | `POST /api/users/import-csv` | `successCount, failureCount, errors: [{ rowNumber, reason }]` | `multipart/form-data: { file }` | Cột CSV bắt buộc: `FullName, Email, Role, Password`. Hiển thị bảng lỗi theo từng dòng nếu có |
| Edit User Modal | `GET /api/users/{userId}` (prefill) → `PUT /api/users/{userId}` (submit) | `fullName, email, role, profile: { department } / { major } / null` | `{ fullName, email, department?, major? }` | `role` chỉ hiển thị read-only, không gửi trong request Update |
| Activate/Deactivate Action | `PUT /api/users/{userId}/status` | — | `{ isActive }` | Error cần xử lý và hiện message rõ ràng: `CannotDeactivateSelf`, `LecturerHasActiveCourses`, `StudentHasActiveEnrollment` |

# 3. Course Management

| Màn hình | Endpoint | Field cần cho UI | Request Body | Ghi chú |
| --- | --- | --- | --- | --- |
| Course Grid Screen (Admin) | `GET /api/courses?page=&pageSize=` | `courseId, name, lecturerName, startDate, endDate, status, maxCapacity, enrolledCount`  • `totalCount` | — | ✅ Đã hỗ trợ `?keyword=&status=`  |
| Course Grid Screen (Lecturer) | `GET /api/courses/my-courses?page=&pageSize=` | Tương tự trên | — | Vừa tách ra ở bước trước — cùng hỗ trợ `?keyword=&status=`  |
| Create Course Modal | `POST /api/courses` | Cần thêm `GET /api/users/lecturers?search=&page=&pageSize=` để lấy danh sách Lecturer cho Modal chọn Lecturer (có Search Bar) | `{ name, description?, startDate, endDate, maxCapacity, lecturerId }` | luôn ép `role=Lecturer`  • `isActive=true` ở Backend (không nhận từ Client). Error: `LecturerNotFound`, `LecturerNotActive`, `LecturerScheduleConflict`. Course mới tạo CHƯA có WeeklySlot nào — bắt buộc phải thêm tối thiểu 2 WeeklySlot ngay sau đó (xem bên dưới) mới enroll được |
| Course Detail Screen | `GET /api/courses/{courseId}`  • `GET /api/courses/{courseId}/weekly-slots`  • `GET /api/courses/{courseId}/sessions` | Course: `name, description, lecturerName, startDate, endDate, status, maxCapacity, enrolledCount`. WeeklySlots: `weeklySlotId, dayOfWeek, sessionType`. Sessions: `classSessionId, weeklySlotId, sessionDate, dayOfWeek, sessionType, isPast, isCancelled` | — | 3 lần gọi API riêng biệt để dựng đủ 1 trang Detail. ✅ Endpoint `GET /weekly-slots` đã có (không cần FE tự groupBy từ `/sessions` nữa như trước). FE nên hiển thị buổi `isCancelled = true` khác biệt (vd gạch ngang) trong danh sách ClassSession. |
| Edit Course (trong Detail) | `PUT /api/courses/{courseId}` | — | `{ name, description?, endDate, maxCapacity }` | Error: `CourseAlreadyCompleted`, `MaxCapacityBelowEnrolledCount`. ⚠️ Đổi `endDate` sẽ tự động sinh thêm/hủy bớt ClassSession — FE nên refetch lại `/sessions` sau khi Edit thành công để danh sách hiển thị đúng |
| Replace Lecturer Modal | `PUT /api/courses/{courseId}/lecturer` | Cần `GET /api/users/lecturers?search=&page=&pageSize=` cho Modal chọn Lecturer mới (giống Create Course) | `{ lecturerId }` | Error: `SameLecturer`, `LecturerScheduleConflict`, `CourseAlreadyCompleted` |
| Add WeeklySlot | `POST /api/courses/{courseId}/weekly-slots` | Response trả thêm `generatedSessionCount` — FE nên hiện toast xác nhận số buổi đã tự sinh | `{ dayOfWeek, sessionType }` | `dayOfWeek`: `Monday`…`Sunday`. `sessionType`: `Morning`/`Afternoon`. Không còn chọn `sessionDate` cụ thể. Error: `DuplicateWeeklySlot` |
| Delete WeeklySlot | `DELETE /api/courses/{courseId}/weekly-slots/{weeklySlotId}` | — | — | Error quan trọng cần hiện rõ: `WeeklySlotInUse` (còn Student đang enroll), `MinimumWeeklySlotsRequired` (còn lại &lt; 2 slot) |
| Edit ClassSession (override 1 buổi cụ thể) | `PUT /api/courses/{courseId}/sessions/{sessionId}` | — | `{ sessionDate, sessionType }` | Dùng cho case đặc biệt (vd dời lịch nghỉ lễ) — không đổi WeeklySlot gốc, các tuần khác cùng slot không bị ảnh hưởng. Error: `SessionAlreadyOccurred` |
| Cancel ClassSession (hủy 1 buổi cụ thể) | `DELETE /api/courses/{courseId}/sessions/{sessionId}` | — | — | ⚠️ Soft-cancel (`isCancelled = true`), KHÔNG xóa vật lý — sau khi gọi, refetch lại `/sessions` sẽ vẫn thấy buổi này với `isCancelled = true`, không biến mất khỏi danh sách. Không còn lỗi `MinimumSessionCountViolation` — rule "tối thiểu 2" đã chuyển sang WeeklySlot. Error: `ClassSessionAlreadyCancelled` nếu gọi 2 lần. Dùng cho case hủy riêng lẻ 1 buổi (vd nghỉ lễ), không hủy cả khung giờ lặp lại |

# 4. Enrollment (Student)

| Màn hình | Endpoint | Field cần cho UI | Request Body | Ghi chú |
| --- | --- | --- | --- | --- |
| Available Courses Screen | `GET /api/courses/available?page=&pageSize=` | `courseId, name, lecturerName, startDate, endDate, maxCapacity, enrolledCount` | — | Tự động lọc Upcoming + chưa đăng ký theo token, không cần FE tự lọc |
| Enroll Course Modal | `GET /api/courses/{courseId}/weekly-slots` (chọn) → `POST /api/enrollments` (submit) | Danh sách WeeklySlot để tick chọn (`weeklySlotId, dayOfWeek, sessionType`) | `{ courseId, weeklySlotIds: [id1, id2] }` | ⚠️ ĐÃ ĐỔI: field `sessionIds` → `weeklySlotIds`. Bắt buộc đúng 2 `weeklySlotId` — không bắt buộc khác `sessionType` lúc Enroll. Error quan trọng: `CourseIsFull`, `InvalidSessionCount`, `SessionNotInCourse`, `ScheduleConflict` (trùng thứ/ca với Course khác đã đăng ký) |
| My Courses Screen | `GET /api/enrollments/my-courses?page=&pageSize=` | `enrollmentId, courseId, courseName, status, grade` | — | `grade` là `null` nếu chưa chấm — FE hiện "Chưa có điểm" thay vì để trống |
| Adjust Session Modal | `GET /api/courses/{courseId}/weekly-slots` (chọn) → `PUT /api/enrollments/{enrollmentId}/sessions` | Danh sách WeeklySlot hiện có của Course, đánh dấu 2 slot Student đang giữ | `{ oldWeeklySlotId, newWeeklySlotId }` | không còn mảng positional `sessionIds: [old, new]` — giờ 2 field tường minh, an toàn hơn. UI hiển thị cả 2 WeeklySlot hiện tại (đủ context) nhưng đổi từng slot một — mỗi lần bấm "Đổi ca" chỉ gọi API cho đúng 1 slot. Error: `CourseAlreadyCompleted`, `AdjustSessionTypeDuplicate` (slot mới trùng `SessionType` với slot còn lại), `ScheduleConflict` |

# 5. Grading (Lecturer)

| Màn hình | Endpoint | Field cần cho UI | Request Body | Ghi chú |
| --- | --- | --- | --- | --- |
| Course Student List Screen | `GET /api/courses/{courseId}/enrollments?page=&pageSize=` | `enrollmentId, studentFullName, studentEmail, grade` | — | ✅ Đã hỗ trợ `?keyword=` tìm theo tên/email |
| Grade Input (inline) | `PUT /api/enrollments/{enrollmentId}/grade` | — | `{ grade }` | `grade` 0–10. Sau khi lưu, Backend tự gửi email + SignalR — FE không cần tự trigger gì thêm |

# 6. Schedule & Attendance

| Màn hình | Endpoint | Field cần cho UI | Request Body | Ghi chú |
| --- | --- | --- | --- | --- |
| My Schedule (Lecturer) | `GET /api/schedule/lecturer` | `courseId, courseName, classSessionId, sessionDate, dayOfWeek, sessionType` | — | Trả toàn bộ, không phân trang — FE tự group theo tuần/tháng |
| My Schedule (Student) | `GET /api/schedule/student` | `courseId, courseName, classSessionId, sessionDate, dayOfWeek, sessionType, attendanceStatus` | — | trước đây chỉ trả 2 session đại diện, giờ trả TẤT CẢ ClassSession (mọi tuần trong suốt kỳ) thuộc 2 WeeklySlot đã chọn — FE cần chuẩn bị UI cho danh sách dài hơn nhiều so với thiết kế ban đầu, kèm sẵn `attendanceStatus` để hiện trên timeline |
| Attendance Marking Screen | `GET /api/courses/{courseId}/sessions/{sessionId}/attendances` (load) → `PUT /api/attendances/{attendanceId}` (từng dòng) | `attendanceId, studentFullName, attendanceStatus, markedAt` | `{ attendanceStatus }` | Update từng Student riêng lẻ (không có bulk update endpoint) — FE cần loading state per-row khi lưu |

# 7. Reporting

| Màn hình | Endpoint | Field cần cho UI | Request Body | Ghi chú |
| --- | --- | --- | --- | --- |
| Course Statistics Screen | `GET /api/reports/course-statistics` | `courseName, status, enrolledCount, averageScore` (cho bảng + 2 Bar chart) | — | Không phân trang — trả toàn bộ 1 lần cho cả Grid và Chart. `averageScore` là `null` nếu chưa ai được chấm |
| Course Report Grid Screen | Dùng lại `GET /api/courses` (Admin) hoặc `GET /api/courses/my-courses` (Lecturer) | `courseId, name` (chỉ cần đủ để hiện Grid + điều hướng) | — | Không có API Report Grid riêng — tái dùng danh sách Course đã có ở mục 3 |
| — Tab: Student Grade Report | `GET /api/reports/student-grades/{courseId}` | `studentFullName, studentEmail, grade` | — | Chỉ Admin gọi được — ẩn Tab với Lecturer |
| — Tab: Attendance Report | `GET /api/reports/attendance/{courseId}` | `studentFullName, presentCount, excusedCount, absentCount, attendanceRate` | — | Admin/Lecturer đều gọi được. ⚠️ `attendanceRate` đã tự loại buổi bị Admin hủy (`isCancelled`) khỏi mẫu số ở Backend — nhưng `absentCount` hiển thị vẫn có thể đếm cả buổi đã hủy (giống hệt cách buổi tương lai chưa diễn ra vẫn nằm trong `absentCount`) — FE không nên tự cộng 3 counter này để suy ngược ra tổng số ca, chỉ dùng `attendanceRate` đã tính sẵn |
| — Tab: Score Distribution | `GET /api/reports/score-distribution/{courseId}` | `items: [{ scoreGroup, studentCount, percentage }]` | — | Chỉ Admin. Trả `items: []` nếu chưa ai được chấm — FE hiện "Chưa có dữ liệu" thay vì vẽ Pie rỗng |
| Personal Summary Screen | `GET /api/reports/my-summary` | `overallGpa`  • `items: [{ courseName, status, grade, presentCount, excusedCount, absentCount, attendanceRate }]` | — | `overallGpa` là `null` nếu chưa có điểm nào. `attendanceRate` đã loại buổi bị hủy khỏi mẫu số đúng như Attendance Report ở mục trên |

# 8. System Jobs

Không có API cho FE — UC-34, UC-35 chạy nền qua Hangfire.