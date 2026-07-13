namespace EnrollmentManagement.Application.Dev.SeedDemoEnrollmentData;

// Input: 6 Course Completed/Active theo thứ tự [Completed x3, Active x3], mỗi Course kèm ĐÚNG cặp
// WeeklySlot (1 Sáng + 1 Chiều) mà Seeder enroll Student vào. DevController map từ output của
// ResetDemoCourseData (Course BC) sang đây.
public sealed record DemoEnrollTarget(
    Guid CourseId,
    bool IsCompleted,
    Guid MorningSlotId,
    Guid AfternoonSlotId);

public sealed record SeedDemoEnrollmentDataOutputDto(int CreatedEnrollmentCount, int CreatedAttendanceCount);