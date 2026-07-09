using CourseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Reporting.Application.Common.Interfaces;

namespace CourseManagement.Infrastructure.Services;

// Reporting BC inject interface này để query SessionDate của ClassSession (Course Context
// sở hữu data này) phục vụ tính AttendanceRate, mà không cần Reporting tự lưu lại SessionDate
// trong Projection của mình (tránh trùng Source of Truth).
public class CourseReportingService : ICourseReportingService {
    private readonly CourseDbContext _context;

    public CourseReportingService(CourseDbContext context) {
        _context = context;
    }

    // "Ended" = đã diễn ra VÀ chưa bị hủy. Buổi bị Admin cancel (vd nghỉ lễ) được loại khỏi
    // mẫu số attendanceRate giống hệt cách buổi tương lai bị loại — không quan tâm Attendance
    // status hiện tại của buổi đó là gì, chỉ đơn giản không tính buổi đó vào tổng số ca.
    public async Task<int> GetEndedSessionCountAsync(
        Guid courseId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) {
        return await _context.ClassSessions
            .CountAsync(s =>
                    s.CourseId == courseId &&
                    s.SessionDate <= asOfDate &&
                    !s.IsCancelled,
                cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetEndedSessionCountsAsync(
        IReadOnlyList<Guid> courseIds,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) {
        return await _context.ClassSessions
            .Where(s =>
                courseIds.Contains(s.CourseId) &&
                s.SessionDate <= asOfDate &&
                !s.IsCancelled)
            .GroupBy(s => s.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);
    }
}