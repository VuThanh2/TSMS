namespace Reporting.Application.Common.Interfaces;

// Cross-BC interface do Reporting Application định nghĩa, chỉ chứa method
public interface ICourseReportingService {
    // Đếm số ClassSession của 1 Course có SessionDate <= asOfDate.
    // Dùng cho GetCourseAttendanceReport — chỉ cần 1 Course mỗi lần gọi.
    Task<int> GetEndedSessionCountAsync(
        Guid courseId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    // Đếm số ClassSession có SessionDate <= asOfDate cho nhiều Course cùng lúc.
    // Dùng cho GetMyPersonalSummary — Student có thể đã đăng ký nhiều Course
    Task<IReadOnlyDictionary<Guid, int>> GetEndedSessionCountsAsync(
        IReadOnlyList<Guid> courseIds,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}