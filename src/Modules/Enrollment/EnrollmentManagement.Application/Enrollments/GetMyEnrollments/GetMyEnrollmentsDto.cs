namespace EnrollmentManagement.Application.Enrollments.GetMyEnrollments;

public sealed record GetMyEnrollmentsOutputDto(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseName,
    string Status,
    decimal? Grade,
    // 2 WeeklySlot Student đang học của enrollment này — FE dùng để lọc sẵn danh sách "Current
    // session" trong Modal AdjustSession (chỉ hiện slot đang học, đỡ phải nhớ/tìm).
    IReadOnlyList<Guid> EnrolledWeeklySlotIds);