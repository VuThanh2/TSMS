using EnrollmentManagement.Application.Common.Interfaces;

namespace Enrollment.UnitTests.Fakes;

// Fake tối giản cho IStudentEnrollmentService (cross-BC Identity→Enrollment) — chỉ implement
// GetEmailsAsync, đủ cho test SendSessionReminderJobService. Emails chứa CHỈ student có email
// hợp lệ + đang active (mô phỏng đúng filter của implementation thật ở Identity BC).
public sealed class FakeStudentEnrollmentService : IStudentEnrollmentService {
    public Dictionary<Guid, string> Emails { get; set; } = new();

    public Task<bool> IsActiveStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<string?> GetFullNameAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<string?> GetEmailAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
        IReadOnlyList<Guid> studentIds, CancellationToken cancellationToken = default) {
        IReadOnlyDictionary<Guid, string> result = studentIds
            .Where(Emails.ContainsKey)
            .Distinct()
            .ToDictionary(id => id, id => Emails[id]);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Guid>> GetActiveStudentIdsAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
