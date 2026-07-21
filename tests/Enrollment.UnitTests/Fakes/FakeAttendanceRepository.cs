using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Repositories;

namespace Enrollment.UnitTests.Fakes;

// Fake in-memory cho IAttendanceRepository — backing 1 List; ghi lại Add/AddRange/RemoveRange
// để test EnrollCourse (pre-populate) và AdjustSession (đồng bộ buổi tương lai) assert được.
public sealed class FakeAttendanceRepository : IAttendanceRepository {
    private readonly List<Attendance> _attendances;

    public FakeAttendanceRepository(IEnumerable<Attendance>? attendances = null) {
        _attendances = attendances?.ToList() ?? new List<Attendance>();
    }

    public List<Attendance> Added { get; } = new();
    public List<Attendance> Removed { get; } = new();

    public Task<Attendance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attendances.FirstOrDefault(a => a.Id == id));

    public Task<Attendance?> GetByStudentAndSessionAsync(
        Guid studentId, Guid classSessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attendances.FirstOrDefault(
            a => a.StudentId == studentId && a.ClassSessionId == classSessionId));

    public Task<List<Attendance>> GetBySessionIdAsync(
        Guid classSessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attendances.Where(a => a.ClassSessionId == classSessionId).ToList());

    public Task<List<Attendance>> GetByStudentAndCourseAsync(
        Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attendances
            .Where(a => a.StudentId == studentId && a.CourseId == courseId).ToList());

    // Không dùng trong các test hiện tại — trả rỗng thay vì throw để không vướng inspection commit.
    public Task<List<SessionAttendanceCount>> GetSessionCountsByCourseAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<SessionAttendanceCount>());

    public Task<List<Attendance>> GetByStudentAndSessionIdsAsync(
        Guid studentId, IReadOnlyList<Guid> classSessionIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_attendances
            .Where(a => a.StudentId == studentId && classSessionIds.Contains(a.ClassSessionId))
            .ToList());

    public void Add(Attendance attendance) {
        _attendances.Add(attendance);
        Added.Add(attendance);
    }

    public void AddRange(IEnumerable<Attendance> attendances) {
        var list = attendances.ToList();
        _attendances.AddRange(list);
        Added.AddRange(list);
    }

    public void RemoveRange(IEnumerable<Attendance> attendances) {
        var list = attendances.ToList();
        foreach (var a in list)
            _attendances.Remove(a);
        Removed.AddRange(list);
    }
}
