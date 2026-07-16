using CourseManagement.Domain.Entities;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using CourseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Primitives;

namespace CourseManagement.Infrastructure.Repositories;

public class CourseRepository : ICourseRepository {
    private readonly CourseDbContext _context;

    public CourseRepository(CourseDbContext context) {
        _context = context;
    }

    public async Task<Course?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public void Remove(Course course) {
        _context.Courses.Remove(course);
    }

    public async Task<Course?> GetByIdWithSessionsAsync(
        Guid id,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .Include(c => c.WeeklySlots)
            .Include(c => c.ClassSessions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Course?> GetByIdWithWeeklySlotsAsync(
        Guid id,
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .Include(c => c.WeeklySlots)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default) {
        var idList = ids.ToList();
        return await _context.Courses
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    // Whitelist cột được phép ORDER BY.
    // Name/StartDate/EndDate là getter computed trên private field (Course.cs) nên KHÔNG map
    // sang cột — phải đi qua EF.Property vào backing field, giống cách keyword search đang làm.
    // Cố tình KHÔNG có lecturerName/enrolledCount: hai cột đó do BC khác sở hữu và chỉ được
    // enrich sau khi phân trang, muốn ORDER BY chúng thì phải JOIN cross-BC — điều bị cấm.
    private static IOrderedQueryable<Course> ApplySort(IQueryable<Course> query, SortInput? sort) {
        var descending = sort?.IsDescending ?? false;

        var ordered = sort?.SortBy?.Trim().ToLowerInvariant() switch {
            "name" => query.OrderByDirection(c => EF.Property<string>(c, "_courseName"), descending),
            "startdate" => query.OrderByDirection(c => EF.Property<DateOnly>(c, "_startDate"), descending),
            "enddate" => query.OrderByDirection(c => EF.Property<DateOnly>(c, "_endDate"), descending),
            // Status map HasConversion<string> → ORDER BY theo bảng chữ cái
            // (Active → Completed → Upcoming), KHÔNG theo thứ tự vòng đời.
            "status" => query.OrderByDirection(c => c.Status, descending),
            _ => query.OrderByDescending(c => c.CreatedAt),
        };

        // Tiebreaker: giữ thứ tự ổn định giữa các trang khi giá trị sort trùng nhau
        // (VD lọc status=Active thì mọi row đều bằng nhau ở cột Status).
        return ordered.ThenBy(c => c.Id);
    }

    public async Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        CourseStatus? status,
        Guid? lecturerId,
        int page,
        int pageSize,
        SortInput? sort = null,
        CancellationToken cancellationToken = default) {
        var query = _context.Courses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword)) {
            var term = keyword.Trim();
            // EF translates shadow property access via EF.Property for backing fields.
            // COLLATE ..._CI_AI: CI bỏ qua hoa/thường, AI bỏ qua dấu (gõ "Vu" khớp "Vũ").
            query = query.Where(c =>
                EF.Functions.Collate(EF.Property<string>(c, "_courseName"), "SQL_Latin1_General_CP1_CI_AI")
                    .Contains(term));
        }

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (lecturerId.HasValue)
            query = query.Where(c => c.LecturerId == lecturerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ApplySort(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Course>> GetByLecturerIdAsync(
        Guid lecturerId,
        CourseStatus? status = null,
        CancellationToken cancellationToken = default) {
        var query = _context.Courses
            .Where(c => c.LecturerId == lecturerId);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetActiveTransitionCandidatesAsync(
        CancellationToken cancellationToken = default) {
        return await _context.Courses
            .Where(c => c.Status == CourseStatus.Upcoming || c.Status == CourseStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public void Add(Course course) {
        _context.Courses.Add(course);
    }

    public void Update(Course course) {
        _context.Courses.Update(course);
    }

    public void AddWeeklySlot(WeeklySlot weeklySlot) {
        _context.WeeklySlots.Add(weeklySlot);
    }

    public void AddClassSession(ClassSession classSession) {
        _context.ClassSessions.Add(classSession);
    }

    public void AddClassSessions(IEnumerable<ClassSession> classSessions) {
        _context.ClassSessions.AddRange(classSessions);
    }
}