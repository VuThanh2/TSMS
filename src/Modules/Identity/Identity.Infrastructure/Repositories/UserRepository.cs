using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

// UserManager.CreateAsync / UpdateAsync / DeleteAsync không được expose ở đây —
// Application Layer gọi UserManager trực tiếp cho các write operations đó.
// Repository chỉ cung cấp các query methods mà UserManager không cover tốt.
public class UserRepository : IUserRepository {
    private readonly Persistence.IdentityDbContext _context;

    public UserRepository(Persistence.IdentityDbContext context) {
        _context = context;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        return await _context.Users
            .Include(u => u.LecturerProfile)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) {
        var normalized = email.Trim().ToUpperInvariant();
        return await _context.Users
            .Include(u => u.LecturerProfile)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default) {
        var normalized = email.Trim().ToUpperInvariant();
        return await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalized
                           && (excludeUserId == null || u.Id != excludeUserId.Value),
                cancellationToken);
    }

    public async Task<(IReadOnlyList<AppUser> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        UserRole? role,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        var query = _context.Users
            .Include(u => u.LecturerProfile)
            .Include(u => u.StudentProfile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword)) {
            var lower = keyword.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(lower) ||
                (u.Email != null && u.Email.ToLower().Contains(lower)));
        }

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Update(AppUser user) {
        _context.Users.Update(user);
    }
}