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

    public async Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.WeeklySlots
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.SessionType)
            .Select(s => new WeeklySlotLookup(
                s.Id,
                s.CourseId,
                s.DayOfWeek.ToString(),
                s.SessionType.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default) {
        return await _context.WeeklySlots
            .Where(s => courseIds.Contains(s.CourseId))
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.SessionType)
            .Select(s => new WeeklySlotLookup(
                s.Id,
                s.CourseId,
                s.DayOfWeek.ToString(),
                s.SessionType.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassSessionLookup?> GetClassSessionAsync(
        Guid classSessionId,
        CancellationToken cancellationToken = default) {
        return await _context.ClassSessions
            .Where(s => s.Id == classSessionId)
            .Select(s => new ClassSessionLookup(
                s.Id,
                s.CourseId,
                s.WeeklySlotId,
                s.SessionDate,
                s.SessionType.ToString(),
                s.IsCancelled))
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
                s.WeeklySlotId,
                s.SessionDate,
                s.SessionType.ToString(),
                s.IsCancelled))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByWeeklySlotIdsAsync(
        IReadOnlyList<Guid> weeklySlotIds,
        CancellationToken cancellationToken = default) {
        return await _context.ClassSessions
            .Where(s => weeklySlotIds.Contains(s.WeeklySlotId))
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.SessionType)
            .Select(s => new ClassSessionLookup(
                s.Id,
                s.CourseId,
                s.WeeklySlotId,
                s.SessionDate,
                s.SessionType.ToString(),
                s.IsCancelled))
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
                s.WeeklySlotId,
                s.SessionDate,
                s.SessionType.ToString(),
                s.IsCancelled))
            .ToListAsync(cancellationToken);
    }
}