using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentManagement.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository {
    private readonly EnrollmentDbContext _context;

    public EnrollmentRepository(EnrollmentDbContext context) {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Include(e => e.EnrolledSessions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Enrollment?> GetByStudentAndCourseAsync(
        Guid studentId, Guid courseId, CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Include(e => e.EnrolledSessions)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);
    }

    public async Task<List<Enrollment>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Include(e => e.EnrolledSessions)
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Enrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .Include(e => e.EnrolledSessions)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveEnrollmentsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId, cancellationToken);
    }

    public void Add(Domain.Entities.Enrollment enrollment) {
        _context.Enrollments.Add(enrollment);
    }
}