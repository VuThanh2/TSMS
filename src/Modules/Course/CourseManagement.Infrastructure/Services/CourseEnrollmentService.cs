using CourseManagement.Domain.ValueObjects;
using CourseManagement.Infrastructure.Persistence;
using EnrollmentManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Infrastructure.Services;

// Implement ICourseEnrollmentService — cross-BC contract được define ở Enrollment.Application.
// Enrollment BC inject interface này để query dữ liệu Course cần thiết cho enrollment flow.
public class CourseEnrollmentService : ICourseEnrollmentService {
    private readonly CourseDbContext _context;

    public CourseEnrollmentService(CourseDbContext context) {
        _context = context;
    }

    public async Task<bool> IsUpcomingAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .AnyAsync(c => c.Id == courseId && c.Status == CourseStatus.Upcoming,
                cancellationToken);
    }

    public async Task<string?> GetStatusAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => (string?)c.Status.ToString())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> GetMaxCapacityAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => (int?)c.MaxCapacity)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.ClassSessions
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.SessionType)
            .Select(s => new ClassSessionLookup(
                s.Id,
                s.CourseId,
                s.SessionDate,
                s.SessionType.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CourseLookup>> GetCoursesByIdsAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new CourseLookup(c.Id, c.Name, c.LecturerId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CourseLookup>> GetCoursesByLecturerAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .Where(c => c.LecturerId == lecturerId)
            .Select(c => new CourseLookup(c.Id, c.Name, c.LecturerId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default) {
        return await _context.ClassSessions
            .Where(s => courseIds.Contains(s.CourseId))
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.SessionType)
            .Select(s => new ClassSessionLookup(
                s.Id,
                s.CourseId,
                s.SessionDate,
                s.SessionType.ToString()))
            .ToListAsync(cancellationToken);
    }
}