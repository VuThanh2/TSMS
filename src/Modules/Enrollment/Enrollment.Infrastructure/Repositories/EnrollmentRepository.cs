using Enrollment.Domain.Repositories;
using Enrollment.Domain.ValueObjects;
using Enrollment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enrollment.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository {
    private readonly EnrollmentDbContext _context;

    public EnrollmentRepository(EnrollmentDbContext context) {
        _context = context;
    }

    public async Task<Domain.Entities.Enrollment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) {
        // Include EnrolledSessions vì hầu hết operations đều cần chúng
        // (AdjustSession, validation). Load luôn để tránh lazy loading.
        return await _context.Enrollments
            .Include("_enrolledSessions")
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Domain.Entities.Enrollment?> GetByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Include("_enrolledSessions")
            .FirstOrDefaultAsync(
                e => e.StudentId == studentId && e.CourseId == courseId,
                cancellationToken);
    }

    public async Task<List<Domain.Entities.Enrollment>> GetByCourseIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Enrollment>> GetByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveEnrollmentsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .CountAsync(
                e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active,
                cancellationToken);
    }

    public void Add(Domain.Entities.Enrollment enrollment) {
        _context.Enrollments.Add(enrollment);
    }
}