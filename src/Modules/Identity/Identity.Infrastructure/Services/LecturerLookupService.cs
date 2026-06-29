using CourseManagement.Application.Common.Interfaces;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Services;

// Implement ILecturerLookupService — cross-BC contract được define ở CourseManagement.Application.
// Course BC inject interface này để check Lecturer active status và lấy tên.
public class LecturerLookupService : ILecturerLookupService {
    private readonly Persistence.IdentityDbContext _context;

    public LecturerLookupService(Persistence.IdentityDbContext context) {
        _context = context;
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

    public async Task<string?> GetFullNameAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default) {
        return await _context.Users
            .Where(u => u.Id == lecturerId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}