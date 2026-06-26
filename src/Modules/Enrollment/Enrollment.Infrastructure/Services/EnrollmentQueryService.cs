using CourseManagement.Application.Common.Interfaces;
using Enrollment.Domain.ValueObjects;
using Enrollment.Infrastructure.Persistence;
using Identity.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enrollment.Infrastructure.Services;

// Implement 2 cross-BC interfaces — Enrollment BC sở hữu Enrollment data:
//   - CourseManagement.Application.IEnrollmentLookupService : Course BC consume.
//   - Identity.Application.IEnrollmentLookupService         : Identity BC consume.
public class EnrollmentQueryService :
    IEnrollmentCourseService,
    IEnrollmentIdentityService {

    private readonly EnrollmentDbContext _context;

    public EnrollmentQueryService(EnrollmentDbContext context) {
        _context = context;
    }

    // ── CourseManagement.Application.IEnrollmentLookupService

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

    public async Task<IReadOnlyDictionary<Guid, decimal?>> GetGradesByCourseAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => new {
                e.CourseId,
                GradeValue = e.Grade == null ? (decimal?)null : e.Grade.Value
            })
            .ToListAsync(cancellationToken);

        return enrollments.ToDictionary(e => e.CourseId, e => e.GradeValue);
    }

    // ── Identity.Application.IEnrollmentLookupService

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