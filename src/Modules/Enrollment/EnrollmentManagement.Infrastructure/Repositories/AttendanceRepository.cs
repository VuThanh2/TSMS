using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentManagement.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository {
    private readonly EnrollmentDbContext _context;

    public AttendanceRepository(EnrollmentDbContext context) {
        _context = context;
    }

    public async Task<Attendance?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) {
        return await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Attendance?> GetByStudentAndSessionAsync(
        Guid studentId,
        Guid classSessionId,
        CancellationToken cancellationToken = default) {
        return await _context.Attendances
            .FirstOrDefaultAsync(
                a => a.StudentId == studentId && a.ClassSessionId == classSessionId,
                cancellationToken);
    }

    public async Task<List<Attendance>> GetBySessionIdAsync(
        Guid classSessionId,
        CancellationToken cancellationToken = default) {
        return await _context.Attendances
            .Where(a => a.ClassSessionId == classSessionId)
            .OrderBy(a => a.StudentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Attendance>> GetByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Attendances
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SessionAttendanceCount>> GetSessionCountsByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.Attendances
            .Where(a => a.CourseId == courseId)
            .GroupBy(a => a.ClassSessionId)
            .Select(g => new SessionAttendanceCount(
                g.Key,
                g.Count(a => a.Status == AttendanceStatus.Present),
                g.Count(a => a.Status == AttendanceStatus.Excused),
                g.Count(a => a.Status == AttendanceStatus.Absent)))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Attendance>> GetByStudentAndSessionIdsAsync(
        Guid studentId,
        IReadOnlyList<Guid> classSessionIds,
        CancellationToken cancellationToken = default) {
        return await _context.Attendances
            .Where(a => a.StudentId == studentId && classSessionIds.Contains(a.ClassSessionId))
            .ToListAsync(cancellationToken);
    }

    public void Add(Attendance attendance) {
        _context.Attendances.Add(attendance);
    }

    public void AddRange(IEnumerable<Attendance> attendances) {
        _context.Attendances.AddRange(attendances);
    }

    public void RemoveRange(IEnumerable<Attendance> attendances) {
        _context.Attendances.RemoveRange(attendances);
    }
}