using System.Reflection;
using CourseManagement.Domain.ValueObjects;
using CourseManagement.Infrastructure.Persistence;
using CourseManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Course.UnitTests;

// UpdateCourseStatusJobService là Hangfire recurring job (chạy 0h05 hàng ngày) — test bằng cách
// gọi thẳng ExecuteAsync() 1 lần, giống cách test CourseOutboxProcessor.
//
// Course.Create() luôn ép StartDate >= hôm nay (DateRange.Create), nên KHÔNG có cách hợp lệ nào
// qua public API để dựng 1 Course "đã kết thúc từ quá khứ" ngay tại thời điểm viết test — trên
// thực tế điều này chỉ xảy ra vì Course được tạo NHIỀU NGÀY TRƯỚC rồi thời gian trôi qua.
// Codebase hiện không có IDateTimeProvider/Clock abstraction để inject "hôm nay" giả lập, nên test
// dùng reflection set thẳng backing field _startDate/_endDate — mô phỏng đúng trạng thái persisted
// trong DB của 1 Course được tạo từ trước. Không lý tưởng, nhưng là cách duy nhất khả thi hiện tại
// để test edge case ngày tháng của job này mà không cần refactor thêm Clock abstraction.
public class UpdateCourseStatusJobServiceTests {
    private static void SetDates(CourseManagement.Domain.Entities.Course course, DateOnly start, DateOnly end) {
        typeof(CourseManagement.Domain.Entities.Course)
            .GetField("_startDate", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(course, start);
        typeof(CourseManagement.Domain.Entities.Course)
            .GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(course, end);
    }

    private static CourseManagement.Domain.Entities.Course CreateValidCourse() =>
        CourseManagement.Domain.Entities.Course.Create(
            lecturerId: Guid.NewGuid(),
            courseName: CourseManagement.Domain.ValueObjects.CourseName.Create("OOP Advanced").Value,
            description: null,
            dateRange: CourseManagement.Domain.ValueObjects.DateRange.Create(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90))).Value,
            maxCapacity: 30,
            lecturerName: "Nguyen Van A").Value;

    private static CourseDbContext CreateInMemoryContext() {
        var options = new DbContextOptionsBuilder<CourseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CourseDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_CourseWithStartDateTodayOrEarlier_TransitionsUpcomingToActive() {
        await using var context = CreateInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var course = CreateValidCourse();
        SetDates(course, today, today.AddDays(30));
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var job = new UpdateCourseStatusJobService(context, NullLogger<UpdateCourseStatusJobService>.Instance);
        await job.ExecuteAsync();

        var reloaded = await context.Courses.SingleAsync(c => c.Id == course.Id);
        Assert.Equal(CourseStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task ExecuteAsync_CourseWithStartDateInFuture_StaysUpcoming() {
        await using var context = CreateInMemoryContext();
        var course = CreateValidCourse(); // StartDate = tomorrow
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var job = new UpdateCourseStatusJobService(context, NullLogger<UpdateCourseStatusJobService>.Instance);
        await job.ExecuteAsync();

        var reloaded = await context.Courses.SingleAsync(c => c.Id == course.Id);
        Assert.Equal(CourseStatus.Upcoming, reloaded.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ActiveCourseWithEndDateStrictlyBeforeToday_TransitionsToCompleted() {
        await using var context = CreateInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var course = CreateValidCourse();
        SetDates(course, today.AddDays(-30), today.AddDays(-1)); // đã kết thúc hôm qua
        course.TransitionStatus(CourseStatus.Active);
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var job = new UpdateCourseStatusJobService(context, NullLogger<UpdateCourseStatusJobService>.Instance);
        await job.ExecuteAsync();

        var reloaded = await context.Courses.SingleAsync(c => c.Id == course.Id);
        Assert.Equal(CourseStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ActiveCourseWithEndDateExactlyToday_DoesNotCompleteYet() {
        // Ranh giới bất đối xứng có chủ đích: Upcoming→Active kích hoạt khi StartDate <= today
        // (inclusive), nhưng Active→Completed chỉ kích hoạt khi EndDate < today (exclusive) —
        // nghĩa là course vẫn ở trạng thái Active suốt ngày EndDate, chỉ Completed từ hôm sau.
        await using var context = CreateInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var course = CreateValidCourse();
        SetDates(course, today.AddDays(-30), today);
        course.TransitionStatus(CourseStatus.Active);
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var job = new UpdateCourseStatusJobService(context, NullLogger<UpdateCourseStatusJobService>.Instance);
        await job.ExecuteAsync();

        var reloaded = await context.Courses.SingleAsync(c => c.Id == course.Id);
        Assert.Equal(CourseStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task ExecuteAsync_OverdueUpcomingCourse_CascadesThroughActiveToCompletedInSameRun() {
        // Course bị "bỏ sót" nhiều ngày (VD hệ thống downtime) — StartDate và EndDate đều đã qua.
        // Job phải xử lý cả 2 phase trong cùng 1 lần chạy: Upcoming → Active → Completed,
        // không được kẹt ở Active vĩnh viễn chỉ vì đã lỡ mất "ngày" chuyển Active.
        await using var context = CreateInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var course = CreateValidCourse();
        SetDates(course, today.AddDays(-30), today.AddDays(-1)); // Upcoming nhưng cả Start & End đều đã qua
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var job = new UpdateCourseStatusJobService(context, NullLogger<UpdateCourseStatusJobService>.Instance);
        await job.ExecuteAsync();

        var reloaded = await context.Courses.SingleAsync(c => c.Id == course.Id);
        Assert.Equal(CourseStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedCourse_IsIgnoredByBothPhases() {
        await using var context = CreateInMemoryContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var course = CreateValidCourse();
        SetDates(course, today.AddDays(-30), today.AddDays(-1));
        course.TransitionStatus(CourseStatus.Active);
        course.TransitionStatus(CourseStatus.Completed);
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var job = new UpdateCourseStatusJobService(context, NullLogger<UpdateCourseStatusJobService>.Instance);
        var exception = await Record.ExceptionAsync(() => job.ExecuteAsync());

        Assert.Null(exception);
        var reloaded = await context.Courses.SingleAsync(c => c.Id == course.Id);
        Assert.Equal(CourseStatus.Completed, reloaded.Status);
    }
}
