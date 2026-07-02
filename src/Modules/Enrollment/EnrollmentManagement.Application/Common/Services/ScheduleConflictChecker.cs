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
        IReadOnlyList<(DateOnly SessionDate, string SessionType)> candidateSlots,
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

        var otherSessions = await _courseEnrollmentService.GetClassSessionsByCourseIdsAsync(
            otherCourseIds, cancellationToken);

        var occupiedClassSessionIds = otherEnrollments
            .SelectMany(e => e.EnrolledSessions)
            .Select(es => es.ClassSessionId)
            .ToHashSet();

        var occupiedSlots = otherSessions
            .Where(s => occupiedClassSessionIds.Contains(s.ClassSessionId))
            .Select(s => (s.SessionDate, s.SessionType))
            .ToHashSet();

        return candidateSlots.Any(occupiedSlots.Contains);
    }
}