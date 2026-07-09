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
        IReadOnlyList<(DayOfWeek DayOfWeek, string SessionType)> candidateSlots,
        CancellationToken cancellationToken = default) {
        var otherEnrollments = (await _enrollmentRepository.GetByStudentIdAsync(studentId, cancellationToken))
            .Where(e => e.CourseId != excludeCourseId)
            .ToList();

        if (otherEnrollments.Count == 0)
            return false;

        var otherCourseIds = otherEnrollments
            .Select(e => e.CourseId)
            .Distinct()
            .ToList();

        // Lấy toàn bộ WeeklySlot của các Course khác Student đang học trong 1 lần gọi (tránh N+1) —
        // so khớp theo (DayOfWeek, SessionType) vì trùng lịch là trùng LẶP LẠI HÀNG TUẦN, không phải 1 ngày cụ thể.
        var otherCourseSlots = await _courseEnrollmentService.GetWeeklySlotsByCourseIdsAsync(
            otherCourseIds, cancellationToken);

        var occupiedWeeklySlotIds = otherEnrollments
            .SelectMany(e => e.EnrolledSessions)
            .Select(es => es.WeeklySlotId)
            .ToHashSet();

        var occupiedSlots = otherCourseSlots
            .Where(s => occupiedWeeklySlotIds.Contains(s.WeeklySlotId))
            .Select(s => (DayOfWeek: Enum.Parse<DayOfWeek>(s.DayOfWeek), s.SessionType))
            .ToHashSet();

        return candidateSlots.Any(occupiedSlots.Contains);
    }
}