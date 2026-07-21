using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentAggregate = EnrollmentManagement.Domain.Entities.Enrollment;

namespace Enrollment.UnitTests.Fakes;

// Fake in-memory cho IEnrollmentRepository — backing bằng 1 List, đủ cho cả test
// ScheduleConflictChecker (chỉ dùng GetByStudentIdAsync) lẫn test handler (GetByIdAsync,
// GetByStudentAndCourseAsync, Add, CountActiveEnrollmentsAsync). Không mô phỏng tracking/EF.
public sealed class FakeEnrollmentRepository : IEnrollmentRepository {
    private readonly List<EnrollmentAggregate> _enrollments;

    public FakeEnrollmentRepository(IEnumerable<EnrollmentAggregate> enrollments) {
        _enrollments = enrollments.ToList();
    }

    // Ghi lại aggregate đã Add — cho test EnrollCourse assert repository thực sự nhận enrollment mới.
    public List<EnrollmentAggregate> Added { get; } = new();

    public Task<EnrollmentAggregate?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_enrollments.FirstOrDefault(e => e.Id == id));

    public Task<EnrollmentAggregate?> GetByStudentAndCourseAsync(
        Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_enrollments.FirstOrDefault(
            e => e.StudentId == studentId && e.CourseId == courseId));

    public Task<List<EnrollmentAggregate>> GetByCourseIdAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_enrollments.Where(e => e.CourseId == courseId).ToList());

    public Task<List<EnrollmentAggregate>> GetByStudentIdAsync(
        Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_enrollments.Where(e => e.StudentId == studentId).ToList());

    public Task<int> CountActiveEnrollmentsAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_enrollments.Count(
            e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active));

    public void Add(EnrollmentAggregate enrollment) {
        _enrollments.Add(enrollment);
        Added.Add(enrollment);
    }
}
