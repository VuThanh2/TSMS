using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using SharedKernel.Primitives;

namespace Identity.UnitTests.Fakes;

// Fake in-memory cho IUserRepository — chỉ cần GetByIdAsync + Update cho test UpdateUserStatus.
// Các method paging/query khác không thuộc phạm vi test nên để throw (không bị gọi).
public sealed class FakeUserRepository : IUserRepository {
    private readonly List<AppUser> _users;

    public FakeUserRepository(IEnumerable<AppUser>? users = null) {
        _users = users?.ToList() ?? new List<AppUser>();
    }

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

    public Task<bool> ExistsByEmailAsync(
        string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Any(u => u.Email == email && u.Id != excludeUserId));

    // Không dùng trong các test hiện tại — trả rỗng thay vì throw để không vướng inspection commit.
    public Task<(IReadOnlyList<AppUser> Items, int TotalCount)> GetPagedAsync(
        string? keyword, UserRole? role, bool? isActive, int page, int pageSize,
        SortInput? sort = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<AppUser>, int)>(([], 0));

    public void Update(AppUser user) { }
}
