using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.ValueObjects;
using CourseManagement.Infrastructure.Persistence;
using Identity.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Infrastructure.Services;

/// Consumed by Identity BC (deactivation check) and Enrollment BC (enrollment validation).
public class CourseQueryService : ICourseQueryService, ICourseLookupService{
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
        return await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => (CourseStatus?)c.Status)
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
 
    public async Task<bool> HasLecturerSlotConflictAsync(
        Guid lecturerId,
        IReadOnlyList<(DayOfWeek DayOfWeek, SessionType SessionType)> candidateSlots,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeCourseId = null,
        CancellationToken cancellationToken = default) {
        if (candidateSlots.Count == 0)
            return false;

        // Vế 1 (chạy dưới SQL): các Course khác của Lecturer có khoảng ngày GIAO NHAU.
        var query = _context.Courses
            .Where(c => c.LecturerId == lecturerId
                && c.Status != CourseStatus.Completed
                && EF.Property<DateOnly>(c, "_startDate") <= endDate
                && EF.Property<DateOnly>(c, "_endDate") >= startDate);

        if (excludeCourseId.HasValue)
            query = query.Where(c => c.Id != excludeCourseId.Value);

        // Chỉ kéo về (DayOfWeek, SessionType) của đúng những course đã lọc ở vế 1 — mỗi Lecturer
        // chỉ vài course, mỗi course vài slot, nên đây là vài chục dòng. So khớp tuple trong memory
        // vì EF không dịch được `candidateSlots.Any(...)` trên list tuple truyền từ ngoài vào.
        var occupiedSlots = await query
            .SelectMany(c => c.WeeklySlots)
            .Select(s => new { s.DayOfWeek, s.SessionType })
            .ToListAsync(cancellationToken);

        // Vế 2: cùng ngày trong tuần + cùng ca thì mới thực sự đụng nhau.
        var occupied = occupiedSlots
            .Select(s => (s.DayOfWeek, s.SessionType))
            .ToHashSet();

        return candidateSlots.Any(occupied.Contains);
    }
 
    public async Task<bool> HasActiveCoursesByLecturerAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .AnyAsync(c => c.LecturerId == lecturerId
                           && (c.Status == CourseStatus.Upcoming || c.Status == CourseStatus.Active),
                cancellationToken);
    }
 
    // ── ICourseLookupService
 
    public async Task<bool> AreAnyActiveAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .AnyAsync(c => courseIds.Contains(c.Id) && c.Status == CourseStatus.Active,
                cancellationToken);
    }
}