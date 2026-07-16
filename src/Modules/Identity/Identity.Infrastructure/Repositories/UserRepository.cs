using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Primitives;

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

    // Whitelist cột được phép ORDER BY. Chỉ nhận cột thật sự nằm trong schema `identity`
    // nên client không thể dò tên cột DB, và mọi cột ở đây đều có sẵn lúc phân trang.
    // Field lạ → rơi về thứ tự mặc định thay vì ném lỗi: sort là tiện ích hiển thị,
    // không đáng làm hỏng cả trang lưới chỉ vì 1 query param sai.
    private static IOrderedQueryable<AppUser> ApplySort(IQueryable<AppUser> query, SortInput? sort) {
        var descending = sort?.IsDescending ?? false;

        var ordered = sort?.SortBy?.Trim().ToLowerInvariant() switch {
            "fullname" => query.OrderByDirection(u => u.FullName, descending),
            "email" => query.OrderByDirection(u => u.Email, descending),
            // Role được map HasConversion<string> → ORDER BY chạy trên cột nvarchar,
            // tức thứ tự bảng chữ cái (Admin → Lecturer → Student), không phải thứ tự enum.
            "role" => query.OrderByDirection(u => u.Role, descending),
            "isactive" => query.OrderByDirection(u => u.IsActive, descending),
            _ => query.OrderBy(u => u.FullName),
        };

        // Tiebreaker bắt buộc khi có phân trang: các row trùng giá trị sort (VD cùng Role)
        // không có thứ tự xác định trong SQL Server → cùng 1 user có thể hiện ở cả trang 1
        // lẫn trang 2, hoặc biến mất. ThenBy(Id) khoá thứ tự đó lại.
        return ordered.ThenBy(u => u.Id);
    }

    public async Task<(IReadOnlyList<AppUser> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        SortInput? sort = null,
        CancellationToken cancellationToken = default) {
        var query = _context.Users
            .Include(u => u.LecturerProfile)
            .Include(u => u.StudentProfile)
            .AsQueryable();
 
        if (!string.IsNullOrWhiteSpace(keyword)) {
            var term = keyword.Trim();
            // COLLATE ..._CI_AI: CI bỏ qua hoa/thường, AI bỏ qua dấu (gõ "Vu" khớp "Vũ").
            // Không cần .ToLower() nữa vì collation đã lo case-insensitive.
            query = query.Where(u =>
                EF.Functions.Collate(u.FullName, "SQL_Latin1_General_CP1_CI_AI").Contains(term) ||
                (u.Email != null &&
                    EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AI").Contains(term)));
        }
 
        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);
 
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);
 
        var totalCount = await query.CountAsync(cancellationToken);
 
        var items = await ApplySort(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
 
        return (items, totalCount);
    }

    public void Update(AppUser user) {
        _context.Users.Update(user);
    }
}