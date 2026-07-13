using EnrollmentManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentManagement.Infrastructure.Persistence;

public class EnrollmentDataResetter : IEnrollmentDataResetter {
    private readonly EnrollmentDbContext _context;

    public EnrollmentDataResetter(EnrollmentDbContext context) {
        _context = context;
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default) {
        // Attendance không có FK tới Enrollment (AggregateRoot độc lập, tham chiếu ClassSessionId
        // của Course BC theo Id, không navigation) — xóa trước/sau đều được. EnrolledSession có FK
        // tới Enrollment nên phải xóa trước Enrollment.
        await _context.Attendances.ExecuteDeleteAsync(cancellationToken);
        await _context.EnrolledSessions.ExecuteDeleteAsync(cancellationToken);
        await _context.Enrollments.ExecuteDeleteAsync(cancellationToken);
    }
}