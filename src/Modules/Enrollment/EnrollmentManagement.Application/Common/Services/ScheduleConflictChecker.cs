using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.Repositories;

namespace EnrollmentManagement.Application.Common.Services;

// Không cần Infrastructure implementation riêng — class này chỉ compose lại
// IEnrollmentRepository + ICourseEnrollmentService (đều đã là abstraction sẵn có),
// không đụng trực tiếp EF Core/DbContext nên đặt thẳng ở Application Layer (Dependency Inversion).
public sealed class ScheduleConflictChecker : IScheduleConflictChecker {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;

    public ScheduleConflictChecker(
        IEnrollmentRepository enrollmentRepository,
        ICourseEnrollmentService courseEnrollmentService) {
        _enrollmentRepository = enrollmentRepository;
        _courseEnrollmentService = courseEnrollmentService;
    }

    public async Task<bool> HasConflictAsync(
        Guid studentId,
        Guid excludeCourseId,
        DateOnly candidateStartDate,
        DateOnly candidateEndDate,
        IReadOnlyList<(DayOfWeek DayOfWeek, string SessionType)> candidateSlots,
        CancellationToken cancellationToken = default) {
        // KHÔNG lọc ở IEnrollmentRepository.GetByStudentIdAsync: GetMyEnrollments và
        // GetStudentSchedule cũng dùng method đó và cần TOÀN BỘ enrollment. Lọc tại đây.
        var otherEnrollments = (await _enrollmentRepository.GetByStudentIdAsync(studentId, cancellationToken))
            .Where(e => e.CourseId != excludeCourseId)
            .ToList();

        if (otherEnrollments.Count == 0)
            return false;

        var otherCourseIds = otherEnrollments
            .Select(e => e.CourseId)
            .Distinct()
            .ToList();

        // Vế 1 — chỉ giữ Course có khoảng ngày GIAO với Course đang xét. Học "Thứ Hai Sáng" ở kỳ
        // trước rồi lại đăng ký "Thứ Hai Sáng" kỳ này thì không đụng gì nhau; thiếu vế này là
        // slot của mọi Course cũ (kể cả đã Completed) chiếm chỗ vĩnh viễn.
        var otherCourses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            otherCourseIds, cancellationToken);

        var overlappingCourseIds = otherCourses
            .Where(c => c.StartDate <= candidateEndDate && c.EndDate >= candidateStartDate)
            .Select(c => c.CourseId)
            .ToHashSet();

        if (overlappingCourseIds.Count == 0)
            return false;

        // Lấy toàn bộ WeeklySlot của các Course chồng lịch trong 1 lần gọi (tránh N+1).
        var otherCourseSlots = await _courseEnrollmentService.GetWeeklySlotsByCourseIdsAsync(
            overlappingCourseIds.ToList(), cancellationToken);

        // Chỉ tính ca Student THỰC SỰ chọn, không phải mọi ca Course đó mở.
        var occupiedWeeklySlotIds = otherEnrollments
            .Where(e => overlappingCourseIds.Contains(e.CourseId))
            .SelectMany(e => e.EnrolledSessions)
            .Select(es => es.WeeklySlotId)
            .ToHashSet();

        // Vế 2 — so theo (DayOfWeek, SessionType) vì lịch LẶP LẠI HÀNG TUẦN, không phải 1 ngày cụ thể.
        var occupiedSlots = otherCourseSlots
            .Where(s => occupiedWeeklySlotIds.Contains(s.WeeklySlotId))
            .Select(s => (DayOfWeek: Enum.Parse<DayOfWeek>(s.DayOfWeek), s.SessionType))
            .ToHashSet();

        return candidateSlots.Any(occupiedSlots.Contains);
    }
}