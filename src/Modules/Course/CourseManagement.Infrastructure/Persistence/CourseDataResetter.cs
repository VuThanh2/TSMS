using CourseManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Infrastructure.Persistence;

public class CourseDataResetter : ICourseDataResetter {
    private readonly CourseDbContext _context;

    public CourseDataResetter(CourseDbContext context) {
        _context = context;
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default) {
        // Xóa theo thứ tự con → cha: ExecuteDeleteAsync sinh SQL DELETE trực tiếp, không đi qua
        // ChangeTracker nên EF cascade (model-level) không được áp dụng — phải tự đảm bảo thứ tự.
        await _context.ClassSessions.ExecuteDeleteAsync(cancellationToken);
        await _context.WeeklySlots.ExecuteDeleteAsync(cancellationToken);
        await _context.Courses.ExecuteDeleteAsync(cancellationToken);
    }
}