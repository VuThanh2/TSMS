using EnrollmentManagement.Application.Common.Interfaces;

namespace Enrollment.UnitTests.Fakes;

// Fake cho IStudentEnrollmentService (cross-BC Identity→Enrollment). Cover cả test job
// (GetEmailsAsync) lẫn test EnrollCourse (IsActiveStudentAsync, GetFullNameAsync, GetEmailAsync).
public sealed class FakeStudentEnrollmentService : IStudentEnrollmentService {
    public Dictionary<Guid, string> Emails { get; set; } = new();

    // ── Cấu hình cho EnrollCourse
    public bool ActiveStudent { get; set; } = true;
    public string FullName { get; set; } = "Nguyen Van A";
    public string Email { get; set; } = "a.nguyen@example.com";

    public Task<bool> IsActiveStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveStudent);

    public Task<string?> GetFullNameAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(FullName);

    public Task<string?> GetEmailAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(Email);

    public Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
        IReadOnlyList<Guid> studentIds, CancellationToken cancellationToken = default) {
        IReadOnlyDictionary<Guid, string> result = studentIds
            .Where(Emails.ContainsKey)
            .Distinct()
            .ToDictionary(id => id, id => Emails[id]);
        return Task.FromResult(result);
    }

    // Không dùng trong các test hiện tại — trả rỗng thay vì throw để không vướng inspection commit.
    public Task<IReadOnlyList<Guid>> GetActiveStudentIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);
}
