namespace EnrollmentManagement.Domain.Repositories;

public interface IAttendanceRepository {
    Task<Entities.Attendance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

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

    // Dùng cho AdjustSession: lấy Attendance của Student ứng với nhiều ClassSessionId cùng lúc
    // (các buổi tương lai thuộc WeeklySlot cũ) để xóa khi đổi slot.
    Task<List<Entities.Attendance>> GetByStudentAndSessionIdsAsync(
        Guid studentId,
        IReadOnlyList<Guid> classSessionIds,
        CancellationToken cancellationToken = default);

    void Add(Entities.Attendance attendance);

    // Dùng khi pre-populate hàng loạt Attendance sau khi Student enroll hoặc AdjustSession.
    void AddRange(IEnumerable<Entities.Attendance> attendances);

    // Dùng khi AdjustSession: xóa Attendance của các buổi tương lai thuộc WeeklySlot cũ.
    void RemoveRange(IEnumerable<Entities.Attendance> attendances);
}