namespace CourseManagement.Application.Dev.ResetDemoCourseData;

// EnrollableCourses: 6 Course Completed/Active theo ĐÚNG thứ tự [Completed x3, Active x3], mỗi
// Course kèm ĐÚNG cặp WeeklySlot (1 Sáng + 1 Chiều) mà Seeder sẽ enroll Student vào. Chọn tường
// minh thay vì để Enrollment BC đoán từ danh sách slot — vì mỗi Course giờ có NHIỀU slot (để
// Student có lựa chọn khi enroll/adjust), cặp "chuẩn" này giữ phân bổ đều + không trùng lịch cho
// dữ liệu seed.
public sealed record ResetDemoCourseDataOutputDto(
    int CreatedCourseCount,
    int LecturerCount,
    IReadOnlyList<Guid> ActiveCourseIds,
    IReadOnlyList<Guid> CompletedCourseIds,
    IReadOnlyList<DemoSeededCourse> EnrollableCourses);

public sealed record DemoSeededCourse(
    Guid CourseId,
    bool IsCompleted,
    Guid MorningSlotId,
    Guid AfternoonSlotId);