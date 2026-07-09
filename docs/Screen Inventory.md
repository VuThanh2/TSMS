# Screen Inventory

# 1. Authentication

| Màn hình | Role | UC | Mục đích / Chức năng chính |
| --- | --- | --- | --- |
| Login Screen | Public (chưa đăng nhập) | UC-01 | Nhập email/password để đăng nhập; sau khi thành công điều hướng đến trang chính tương ứng với Role |
| Reset Password Screen | Public | UC-04 | Form 1 bước duy nhất: nhập Email + Mật khẩu mới để đặt lại trực tiếp (không có bước xác thực/gửi mã qua email) |
| Logout | Admin, Lecturer, Student | UC-02 | Không phải màn hình riêng — nút trên Navbar, hủy session và quay lại Login |

# 2. User Management (Admin only)

| Màn hình | Role | UC | Mục đích / Chức năng chính |
| --- | --- | --- | --- |
| User List Screen | Admin | UC-05, UC-06 | Xem toàn bộ User dạng bảng (Họ tên, Email, Role, trạng thái); search theo tên/email + filter theo Role; phân trang |
| Create User Modal/Form | Admin | UC-07 | Nhập Họ tên, Email, Role để tạo 1 tài khoản mới với mật khẩu mặc định |
| Import User CSV Modal | Admin | UC-08 | Upload file CSV để tạo hàng loạt tài khoản; hiển thị kết quả số dòng thành công/thất bại kèm lý do lỗi |
| Edit User Modal/Form | Admin | UC-09 | Sửa Họ tên, Email, Profile (Department/Major theo Role); Role hiển thị read-only không cho sửa |
| Activate/Deactivate Action | Admin | UC-10 | Bật/tắt trạng thái hoạt động của 1 User kèm Confirm Dialog; action inline trên User List, không tách màn hình riêng |

# 3. Course Management

| Màn hình | Role | UC | Mục đích / Chức năng chính |
| --- | --- | --- | --- |
| Course Grid Screen | Admin, Lecturer | UC-11, UC-12 | Xem danh sách Course dạng bảng; search theo tên + filter Status. Dùng chung 1 layout nhưng gọi 2 API riêng biệt theo Role: Admin gọi `GET /api/courses` (toàn bộ Course, phân trang), Lecturer gọi `GET /api/courses/my-courses` (chỉ Course mình phụ trách). |
| Create Course Modal/Form | Admin | UC-13 | Nhập Name, Description, StartDate, EndDate, maxCapacity, chọn Lecturer phụ trách để tạo Course mới (mặc định Upcoming) |
| Course Detail Screen | Admin, Lecturer, Student | UC-14 → UC-18 | Trang tổng hợp thông tin 1 Course: thông tin chung, danh sách WeeklySlot (khung giờ lặp lại hàng tuần) và ClassSession đã tự sinh. Là nơi Admin thực hiện các thao tác quản lý bên dưới |
| — Edit Course (trong Course Detail) | Admin | UC-14 | Sửa Name, Description, EndDate, maxCapacity; không cho sửa nếu Course đã Completed |
| — Replace Lecturer Modal | Admin | UC-15 | Chọn Lecturer khác để thay thế Lecturer đang phụ trách; chỉ áp dụng khi Course Upcoming/Active |
| — Add/Delete WeeklySlot (trong Course Detail) | Admin | UC-16, UC-18 | không còn chọn ngày học cụ thể — chỉ chọn Thứ + Ca (Sáng/Chiều), hệ thống tự sinh toàn bộ ClassSession cho cả kỳ. Xóa slot chỉ hủy các buổi tương lai, buổi đã qua giữ nguyên; luôn giữ tối thiểu 2 WeeklySlot; từ chối xóa nếu còn Student đang enroll vào slot đó |
| — Edit/Cancel 1 ClassSession riêng lẻ (trong Course Detail) | Admin | UC-17 | Override 1 buổi cụ thể (vd nghỉ lễ dời ngày hoặc hủy) mà không ảnh hưởng cả WeeklySlot — các tuần khác cùng slot vẫn diễn ra bình thường; không sửa/hủy buổi đã qua ngày. ⚠️ Hủy là soft-cancel (đánh dấu, không xóa) — UI nên hiển thị buổi đã hủy khác biệt (vd gạch ngang) thay vì biến mất khỏi danh sách, vì buổi đã hủy vẫn tồn tại trong response `GET /sessions` |

# 4. Enrollment (Student)

| Màn hình | Role | UC | Mục đích / Chức năng chính |
| --- | --- | --- | --- |
| Available Courses Screen | Student | UC-19 | Xem danh sách Course đang Upcoming mà Student chưa đăng ký, kèm sức chứa/số đã đăng ký, để chọn Course muốn tham gia |
| Enroll Course Modal | Student | UC-20 | chọn đúng 2 WeeklySlot (khung giờ lặp lại hàng tuần, áp dụng cả kỳ — không phải 2 buổi học cụ thể) để hoàn tất đăng ký 1 Course; báo lỗi nếu Course đã đầy (`CourseIsFull`) |
| My Courses Screen | Student | UC-22 | Xem danh sách Course đã đăng ký kèm trạng thái và điểm số (nếu đã có) |
| Adjust Session Modal | Student | UC-21 | hoán đổi 1 WeeklySlot đang học sang 1 WeeklySlot khác (không phải chọn lại tự do 2 slot) cho 1 Course đã đăng ký; chỉ cho phép khi Course chưa Completed. UI hiển thị cả 2 slot hiện tại (Student thấy toàn bộ lịch), nhưng đổi từng slot 1 — khớp đúng backend chỉ nhận 1 cặp OldWeeklySlotId/NewWeeklySlotId mỗi lần gọi |

# 5. Grading (Lecturer)

| Màn hình | Role | UC | Mục đích / Chức năng chính |
| --- | --- | --- | --- |
| Course Student List Screen | Lecturer | UC-23, UC-24 | Xem danh sách Student đã đăng ký trong 1 Course mình phụ trách kèm điểm hiện tại; search theo tên/email trong phạm vi Course đó |
| Grade Input (inline trong Student List) | Lecturer | UC-25 | Nhập/sửa điểm (0–10) cho từng Student; chỉ cho phép khi Course Active/Completed; sau khi lưu hệ thống tự gửi email cho Student (không cần chờ/hiện trên UI) |

# 6. Schedule & Attendance

| Màn hình | Role | UC | Mục đích / Chức năng chính |
| --- | --- | --- | --- |
| My Schedule Screen (Timeline) | Lecturer, Student | UC-26 | Xem lịch dạy (Lecturer) hoặc lịch học (Student) dạng timeline, điều hướng theo tuần/tháng, hiển thị trạng thái điểm danh từng ca |
| Attendance Marking Screen | Lecturer | UC-27 | Chọn 1 ca học cụ thể (từ Course Detail hoặc Course Student List), điểm danh từng Student với 3 trạng thái Present/Absent/Excused; có thể cập nhật lại bất kỳ lúc nào. ⚠️ Nếu ca học đã bị Admin hủy (`isCancelled`), API từ chối `MarkAttendance` (`SessionCancelled`) — UI nên disable nút điểm danh và hiển thị badge "Đã hủy" thay vì cho bấm rồi mới báo lỗi |

# 7. Reporting

| Màn hình | Role | UC | Mục đích / Chức năng chính |
| --- | --- | --- | --- |
| Course Statistics Screen | Admin | UC-28, UC-30 | Trang tổng quan toàn bộ Course, gồm bảng thống kê (tên, trạng thái, số Student đã đăng ký) + 2 Bar chart (mỗi cột = 1 Course, không cần chọn lọc): (1) số lượng Student đã đăng ký của mỗi Course để so sánh độ đông, (2) điểm trung bình của mỗi Course để so sánh kết quả học tập giữa các Course |
| Course Report Grid Screen | Admin, Lecturer | — (màn hình điều hướng) | Grid liệt kê toàn bộ Course (Admin thấy hết, Lecturer chỉ thấy Course mình phụ trách); nhấn vào 1 Course để mở trang chi tiết báo cáo. **Lưu ý về quyền:** cả 3 Tab bên dưới đều thuộc cùng 1 trang chi tiết nhưng phân quyền riêng từng Tab — Admin xem được cả 3 Tab, Lecturer **chỉ xem được 1 Tab (Attendance Report)**, 2 Tab còn lại (Grade Report, Score Distribution) phải ẩn với Lecturer vì API tương ứng chỉ cho phép Admin gọi |
| — Tab: Student Grade Report | Admin | UC-29 | Xem bảng điểm toàn bộ Student đã đăng ký Course đó (Tên, Email, Điểm số) |
| — Tab: Attendance Report | Admin, Lecturer | UC-31 | Xem bảng điểm danh từng Student trong Course đó: số ca Present/Excused/Absent và tỷ lệ điểm danh |
| — Tab: Score Distribution (Pie Chart) | Admin | UC-30 | Pie chart thể hiện tỷ lệ Student theo nhóm điểm (Xuất sắc/Giỏi/Trung bình/Yếu) của riêng Course đang xem; bỏ qua Student chưa có điểm |
| Personal Summary Screen | Student | UC-32 | Xem tổng hợp điểm + tỷ lệ điểm danh của chính mình trên toàn bộ Course đã đăng ký, kèm điểm trung bình chung |
| Real-time Grade Notification | Student | UC-33 | Không phải màn hình riêng — khi Lecturer vừa chấm điểm, tự động cập nhật điểm ngay trên My Courses / Personal Summary nếu Student đang mở trang (qua SignalR) |

> Lưu ý: Course Report Grid Screen dùng chung layout với Course Detail (đều là "click 1 Course để xem chi tiết") nhưng mục đích khác nhau — Course Detail phục vụ quản lý (Edit/Session), Course Report Grid phục vụ xem báo cáo (3 Tab). Có thể cân nhắc gộp thành 1 trang Course Detail với thêm Tab "Reports" nếu muốn giảm số route, tùy quyết định lúc code.
> 

# 8. System Jobs (không có UI)

UC-34 (tự động cập nhật trạng thái Course) và UC-35 (gửi email nhắc ca học) chạy hoàn toàn ở Backend (Hangfire), không cần màn hình Frontend.