namespace Enrollment.Domain.Repositories;

public interface IEnrollmentRepository {
    Task<Entities.Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Dùng để check Student đã enroll Course này chưa (duplicate check).
    Task<Entities.Enrollment?> GetByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    // Dùng để lấy danh sách enrollment của một Course (Admin/Lecturer xem).
    Task<List<Entities.Enrollment>> GetByCourseIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    // Dùng để lấy danh sách enrollment của một Student (Student xem môn học của mình).
    Task<List<Entities.Enrollment>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    // Đếm số enrollment Active của Course — dùng để check MaxCapacity trước khi enroll.
    Task<int> CountActiveEnrollmentsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
}