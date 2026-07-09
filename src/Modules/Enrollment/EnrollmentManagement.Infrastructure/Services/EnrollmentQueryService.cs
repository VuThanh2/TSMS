using CourseManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentManagement.Infrastructure.Persistence;
using Identity.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentManagement.Infrastructure.Services;

// Implement 2 cross-BC interfaces — Enrollment BC sở hữu Enrollment data:
//   - CourseManagement.Application.IEnrollmentCourseService : Course BC consume.
//   - Identity.Application.IEnrollmentIdentityService        : Identity BC consume.
public class EnrollmentQueryService :
    IEnrollmentCourseService,
    IEnrollmentIdentityService {

    private readonly EnrollmentDbContext _context;

    public EnrollmentQueryService(EnrollmentDbContext context) {
        _context = context;
    }

    // ── CourseManagement.Application.IEnrollmentCourseService

    public async Task<int> GetEnrollmentCountAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetEnrolledCourseIdsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);
    }

    // Dùng làm precondition trước khi Course BC cho phép RemoveWeeklySlot — không cho xóa
    // slot đang có Student enroll (Enrollment BC sở hữu dữ liệu EnrolledSession).
    public async Task<bool> IsWeeklySlotInUseAsync(
        Guid weeklySlotId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .AnyAsync(e => e.EnrolledSessions.Any(s => s.WeeklySlotId == weeklySlotId),
                cancellationToken);
    }

    // ── Identity.Application.IEnrollmentIdentityService

    // Chỉ trả về courseIds mà Student đang Active enroll — đúng data Enrollment BC sở hữu.
    // Identity Application handler tự check CourseStatus qua ICourseLookupService.AreAnyActiveAsync.
    public async Task<IReadOnlyList<Guid>> GetActiveCourseIdsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);
    }
}