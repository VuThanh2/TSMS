using EnrollmentManagement.Application.Common.Interfaces;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Services;

// Implement IStudentEnrollmentService — cross-BC contract được define ở Enrollment.Application.
// Enrollment BC inject interface này để check Student active status và lấy thông tin cơ bản.
public class StudentEnrollmentService : IStudentEnrollmentService {
    private readonly Persistence.IdentityDbContext _context;

    public StudentEnrollmentService(Persistence.IdentityDbContext context) {
        _context = context;
    }

    public async Task<bool> IsActiveStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .AnyAsync(u => u.Id == studentId
                           && u.Role == UserRole.Student
                           && u.IsActive,
                cancellationToken);
    }

    public async Task<string?> GetFullNameAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .Where(u => u.Id == studentId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> GetEmailAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .Where(u => u.Id == studentId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
        IReadOnlyList<Guid> studentIds,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .Where(u => studentIds.Contains(u.Id) && u.IsActive && u.Email != null)
            .ToDictionaryAsync(u => u.Id, u => u.Email!, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetActiveStudentIdsAsync(
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .Where(u => u.Role == UserRole.Student && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }
}