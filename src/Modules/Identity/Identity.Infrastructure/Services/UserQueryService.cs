using Identity.Application.Common.Interfaces;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Services;

// Implement IUserQueryService — cross-BC interface được define ở Identity.Application.
public class UserQueryService : IUserQueryService {
    private readonly Persistence.IdentityDbContext _context;
 
    public UserQueryService(Persistence.IdentityDbContext context) {
        _context = context;
    }
 
    public async Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }
 
    public async Task<bool> IsActiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
    }
 
    public async Task<bool> HasRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == userId && u.Role == Enum.Parse<UserRole>(role, ignoreCase: true),
                cancellationToken);
    }
 
    public async Task<string?> GetFullNameAsync(
        Guid userId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}