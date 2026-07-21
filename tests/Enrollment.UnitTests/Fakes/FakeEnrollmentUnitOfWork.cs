using EnrollmentManagement.Application.Common.Interfaces;

namespace Enrollment.UnitTests.Fakes;

// Fake IEnrollmentUnitOfWork — đếm số lần SaveChanges để test khẳng định handler CÓ (hay KHÔNG)
// commit. Không có DB thật nên chỉ trả số row giả (1).
public sealed class FakeEnrollmentUnitOfWork : IEnrollmentUnitOfWork {
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}
