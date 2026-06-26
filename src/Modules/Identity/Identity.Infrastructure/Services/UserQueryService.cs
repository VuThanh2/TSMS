using CourseManagement.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Services;

// Implement IUserQueryService — cross-BC interface được define ở Identity.Application.
// Enrollment BC dùng interface này để check Student/Lecturer existence và role.
// Course BC dùng interface này để check Lecturer active status và lấy tên.
public class UserQueryService : IUserQueryService, ILecturerLookupService {
    private readonly Persistence.IdentityDbContext _context;

    public UserQueryService(Persistence.IdentityDbContext context) {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
    }

    public async Task<bool> HasRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == userId && u.Role == Enum.Parse<UserRole>(role),
                cancellationToken);
    }

    public async Task<string?> GetFullNameAsync(Guid userId, CancellationToken cancellationToken = default) {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user?.FullName;
    }
    
    public async Task<bool> IsActiveLecturerAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == lecturerId
                           && u.Role == UserRole.Lecturer
                           && u.IsActive,
                cancellationToken);
    }
}