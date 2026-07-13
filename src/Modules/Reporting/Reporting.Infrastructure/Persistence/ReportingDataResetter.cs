using Microsoft.EntityFrameworkCore;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Infrastructure.Persistence;

public class ReportingDataResetter : IReportingDataResetter {
    private readonly ReportingDbContext _context;

    public ReportingDataResetter(ReportingDbContext context) {
        _context = context;
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default) {
        // Cả 5 ReadModel đều độc lập (không FK lẫn nhau) — thứ tự xóa không quan trọng.
        await _context.CourseStatistics.ExecuteDeleteAsync(cancellationToken);
        await _context.StudentGradeReports.ExecuteDeleteAsync(cancellationToken);
        await _context.ScoreDistributions.ExecuteDeleteAsync(cancellationToken);
        await _context.AttendanceReports.ExecuteDeleteAsync(cancellationToken);
        await _context.PersonalSummaries.ExecuteDeleteAsync(cancellationToken);
    }
}