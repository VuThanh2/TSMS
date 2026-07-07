using EnrollmentManagement.Application.Common.Interfaces;

namespace Enrollment.UnitTests.Fakes;

// Fake tối giản cho ICourseEnrollmentService (cross-BC interface Course→Enrollment) — chỉ
// implement GetWeeklySlotsByCourseIdsAsync, đủ cho test ScheduleConflictChecker.
public sealed class FakeCourseEnrollmentService : ICourseEnrollmentService {
    private readonly List<WeeklySlotLookup> _weeklySlots;

    public FakeCourseEnrollmentService(IEnumerable<WeeklySlotLookup> weeklySlots) {
        _weeklySlots = weeklySlots.ToList();
    }

    public Task<bool> IsUpcomingAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<string?> GetStatusAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<int?> GetMaxCapacityAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WeeklySlotLookup>>(
            _weeklySlots.Where(s => courseIds.Contains(s.CourseId)).ToList());

    public Task<ClassSessionLookup?> GetClassSessionAsync(
        Guid classSessionId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByWeeklySlotIdsAsync(
        IReadOnlyList<Guid> weeklySlotIds, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<CourseLookup>> GetCoursesByIdsAsync(
        IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<CourseLookup>> GetCoursesByLecturerAsync(
        Guid lecturerId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
