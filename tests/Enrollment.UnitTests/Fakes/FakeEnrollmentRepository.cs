using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Repositories;

namespace Enrollment.UnitTests.Fakes;

// Fake tối giản cho IEnrollmentRepository — chỉ implement đủ để test ScheduleConflictChecker
// (chỉ dùng GetByStudentIdAsync), các method khác không được gọi trong scope test này.
public sealed class FakeEnrollmentRepository : IEnrollmentRepository {
    private readonly List<EnrollmentManagement.Domain.Entities.Enrollment> _enrollments;

    public FakeEnrollmentRepository(IEnumerable<EnrollmentManagement.Domain.Entities.Enrollment> enrollments) {
        _enrollments = enrollments.ToList();
    }

    public Task<EnrollmentManagement.Domain.Entities.Enrollment?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<EnrollmentManagement.Domain.Entities.Enrollment?> GetByStudentAndCourseAsync(
        Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<EnrollmentManagement.Domain.Entities.Enrollment>> GetByCourseIdAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<EnrollmentManagement.Domain.Entities.Enrollment>> GetByStudentIdAsync(
        Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_enrollments.Where(e => e.StudentId == studentId).ToList());

    public Task<int> CountActiveEnrollmentsAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public void Add(EnrollmentManagement.Domain.Entities.Enrollment enrollment) =>
        throw new NotImplementedException();
}
