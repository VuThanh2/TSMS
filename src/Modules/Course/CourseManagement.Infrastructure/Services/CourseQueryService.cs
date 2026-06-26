using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.ValueObjects;
using CourseManagement.Infrastructure.Persistence;
using Enrollment.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Infrastructure.Services;

/// Consumed by Identity BC (deactivation check) and Enrollment BC (enrollment validation).
public class CourseQueryService : ICourseQueryService, ICourseLookupService, ICourseEnrollmentService {
    private readonly CourseDbContext _context;

    public CourseQueryService(CourseDbContext context) {
        _context = context;
    }

    public async Task<bool> IsUpcomingAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .AnyAsync(c => c.Id == courseId && c.Status == CourseStatus.Upcoming,
                cancellationToken);
    }

    public async Task<CourseStatus?> GetStatusAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        var course = await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Status })
            .FirstOrDefaultAsync(cancellationToken);

        return course?.Status;
    }

    public async Task<int?> GetMaxCapacityAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        var course = await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new { c.MaxCapacity })
            .FirstOrDefaultAsync(cancellationToken);

        return course?.MaxCapacity;
    }

    public async Task<bool> HasOverlappingCourseAsync(
        Guid lecturerId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeCourseId = null,
        CancellationToken cancellationToken = default) {
        var query = _context.Courses
            .Where(c => c.LecturerId == lecturerId
                && c.Status != CourseStatus.Completed
                // Overlap condition: existing.StartDate <= newEndDate && existing.EndDate >= newStartDate
                && EF.Property<DateOnly>(c, "_startDate") <= endDate
                && EF.Property<DateOnly>(c, "_endDate") >= startDate);

        if (excludeCourseId.HasValue)
            query = query.Where(c => c.Id != excludeCourseId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasActiveCoursesByLecturerAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .AnyAsync(c => c.LecturerId == lecturerId
                           && (c.Status == CourseStatus.Upcoming || c.Status == CourseStatus.Active),
                cancellationToken);
    }
 
    // Identity BC dùng để check "courseIds này có cái nào đang Active không?"
    public async Task<bool> AreAnyActiveAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .AnyAsync(c => courseIds.Contains(c.Id) && c.Status == CourseStatus.Active,
                cancellationToken);
    }
    
    // ── ICourseEnrollmentService
 
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
}