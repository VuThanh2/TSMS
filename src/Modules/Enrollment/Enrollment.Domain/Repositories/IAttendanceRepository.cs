namespace Enrollment.Domain.Repositories;

public interface IAttendanceRepository {
    Task<Entities.Attendance?> GetByStudentAndSessionAsync(
        Guid studentId,
        Guid classSessionId,
        CancellationToken cancellationToken = default);

    // Dùng cho GetSessionAttendances: Lecturer xem điểm danh cả lớp trong 1 buổi.
    Task<List<Entities.Attendance>> GetBySessionIdAsync(
        Guid classSessionId,
        CancellationToken cancellationToken = default);

    // Dùng cho Schedule query: lấy attendance của Student trong 1 Course.
    Task<List<Entities.Attendance>> GetByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    void Add(Entities.Attendance attendance);

    // Dùng khi pre-populate hàng loạt Attendance sau khi Student enroll.
    void AddRange(IEnumerable<Entities.Attendance> attendances);
}