using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Repositories;

/// Note: Create / password operations are delegated to ASP.NET Core Identity's
/// UserManager — this repository only exposes query methods that UserManager
/// does not cover efficiently
public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AppUser> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        UserRole? role,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Update(AppUser user);
}