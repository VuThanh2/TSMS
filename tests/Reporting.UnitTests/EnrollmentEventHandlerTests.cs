using EnrollmentManagement.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Reporting.Domain.ReadModels;
using Reporting.Infrastructure.EventHandlers;
using Reporting.Infrastructure.Persistence;
using Reporting.Infrastructure.Repositories;

namespace Reporting.UnitTests;

// Outbox là at-least-once delivery: một message có thể được publish lại (job chạy chồng lấn,
// hoặc retry sau lỗi tạm thời) trước khi được đánh dấu ProcessedOn. EventHandler vì vậy PHẢI
// idempotent với message trùng — nếu không, side-effect dạng "cộng dồn" (increment counter)
// sẽ sai lệch dữ liệu dù không có exception nào xảy ra.
public class EnrollmentEventHandlerTests {
    private static (ReportingDbContext Context, EnrollmentEventHandler Handler) CreateSut() {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ReportingDbContext(options);
        var repository = new ReportingRepository(context);
        var handler = new EnrollmentEventHandler(repository, NullLogger<EnrollmentEventHandler>.Instance);
        return (context, handler);
    }

    private static StudentEnrolledEvent CreateStudentEnrolledEvent(Guid enrollmentId, Guid courseId) =>
        StudentEnrolledEvent.Create(
            enrollmentId: enrollmentId,
            studentId: Guid.NewGuid(),
            courseId: courseId,
            enrolledAt: DateTime.UtcNow,
            studentFullName: "Tran Thi B",
            studentEmail: "b.tran@example.com",
            courseName: "OOP Advanced",
            courseStatus: "Upcoming",
            totalSessionsInCourse: 20);

    [Fact]
    public async Task Handle_StudentEnrolledEvent_InitializesAllFourProjectionsOnce() {
        var (context, handler) = CreateSut();
        var courseId = Guid.NewGuid();
        context.CourseStatistics.Add(CourseStatisticsView.Create(
            courseId: courseId,
            lecturerId: Guid.NewGuid(),
            courseName: "OOP Advanced",
            lecturerName: "Nguyen Van A",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            status: "Upcoming"));
        await context.SaveChangesAsync();

        var enrollmentId = Guid.NewGuid();
        await handler.Handle(CreateStudentEnrolledEvent(enrollmentId, courseId), CancellationToken.None);

        var stats = await context.CourseStatistics.SingleAsync(v => v.CourseId == courseId);
        Assert.Equal(1, stats.EnrolledCount);
        Assert.Equal(1, stats.UngradedStudentCount);
        Assert.Single(context.StudentGradeReports);
        Assert.Single(context.AttendanceReports);
        Assert.Single(context.PersonalSummaries);
    }

    [Fact]
    public async Task Handle_StudentEnrolledEvent_WhenRedelivered_DoesNotDoubleCountEnrolledCount() {
        // Arrange — mô phỏng at-least-once delivery: cùng 1 StudentEnrolledEvent (cùng EnrollmentId)
        // được OutboxProcessor publish 2 lần (do OutboxProcessor retry hoặc 2 lần Hangfire chạy chồng).
        var (context, handler) = CreateSut();
        var courseId = Guid.NewGuid();
        context.CourseStatistics.Add(CourseStatisticsView.Create(
            courseId: courseId,
            lecturerId: Guid.NewGuid(),
            courseName: "OOP Advanced",
            lecturerName: "Nguyen Van A",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            status: "Upcoming"));
        await context.SaveChangesAsync();

        var enrollmentId = Guid.NewGuid();
        var domainEvent = CreateStudentEnrolledEvent(enrollmentId, courseId);

        // Act — handle đúng event 2 lần liên tiếp.
        await handler.Handle(domainEvent, CancellationToken.None);
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert — EnrolledCount phải vẫn là 1, không phải 2. Trước fix: bug này khiến
        // CourseStatisticsView.EnrolledCount lệch khỏi số dòng StudentGradeReportView thật.
        var stats = await context.CourseStatistics.SingleAsync(v => v.CourseId == courseId);
        Assert.Equal(1, stats.EnrolledCount);
        Assert.Equal(1, stats.UngradedStudentCount);

        // Các projection khác vốn đã idempotent — vẫn phải đúng 1 dòng mỗi loại.
        Assert.Single(context.StudentGradeReports);
        Assert.Single(context.AttendanceReports);
        Assert.Single(context.PersonalSummaries);
    }

    [Fact]
    public async Task Handle_GradeAssignedEvent_WhenCourseStatisticsMissing_DoesNotThrow() {
        // CourseStatisticsView có thể chưa tồn tại nếu CourseCreatedEvent chưa được xử lý xong
        // (event tới không đúng thứ tự — Outbox không đảm bảo strict ordering giữa 2 BC khác nhau).
        var (context, handler) = CreateSut();
        var enrollmentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        context.StudentGradeReports.Add(StudentGradeReportView.Create(
            enrollmentId: enrollmentId,
            courseId: courseId,
            courseName: "OOP Advanced",
            studentId: Guid.NewGuid(),
            studentFullName: "Tran Thi B",
            studentEmail: "b.tran@example.com"));
        await context.SaveChangesAsync();

        var gradeAssigned = GradeAssignedEvent.Create(
            enrollmentId: enrollmentId,
            studentId: Guid.NewGuid(),
            courseId: courseId,
            grade: 8.5m);

        var exception = await Record.ExceptionAsync(
            () => handler.Handle(gradeAssigned, CancellationToken.None));

        Assert.Null(exception);
        var gradeReport = await context.StudentGradeReports.SingleAsync(v => v.EnrollmentId == enrollmentId);
        Assert.Equal(8.5m, gradeReport.Grade);
    }

    [Fact]
    public async Task Handle_GradeAssignedEvent_RebuildsScoreDistribution_RespectingBoundaryValues() {
        // Boundary theo CourseScoreDistributionView.ResolveScoreGroup:
        // 9.0 → Xuất sắc (>=9), 7.0 → Giỏi (>=7, <9), 5.0 → Trung bình (>=5, <7), 4.9 → Yếu (<5).
        var (context, handler) = CreateSut();
        var courseId = Guid.NewGuid();

        context.CourseStatistics.Add(CourseStatisticsView.Create(
            courseId: courseId,
            lecturerId: Guid.NewGuid(),
            courseName: "OOP Advanced",
            lecturerName: "Nguyen Van A",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            status: "Upcoming"));

        foreach (var (group, start, end) in ScoreGroups.All)
            context.ScoreDistributions.Add(CourseScoreDistributionView.Create(courseId, "OOP Advanced", group, start, end));

        var grades = new[] { 9.0m, 7.0m, 5.0m, 4.9m };
        var enrollmentIds = new List<Guid>();
        foreach (var grade in grades) {
            var enrollmentId = Guid.NewGuid();
            enrollmentIds.Add(enrollmentId);
            context.StudentGradeReports.Add(StudentGradeReportView.Create(
                enrollmentId: enrollmentId,
                courseId: courseId,
                courseName: "OOP Advanced",
                studentId: Guid.NewGuid(),
                studentFullName: $"Student {grade}",
                studentEmail: $"student{grade}@example.com"));
        }
        await context.SaveChangesAsync();

        // Act — chấm điểm lần lượt cho từng student (mỗi lần trigger rebuild toàn bộ distribution).
        for (var i = 0; i < grades.Length; i++) {
            await handler.Handle(
                GradeAssignedEvent.Create(enrollmentIds[i], Guid.NewGuid(), courseId, grades[i]),
                CancellationToken.None);
        }

        // Assert — mỗi score group có đúng 1 student, percentage = 1/4 = 25%.
        var distributions = await context.ScoreDistributions
            .Where(v => v.CourseId == courseId)
            .ToDictionaryAsync(v => v.ScoreGroup);

        Assert.Equal(1, distributions[ScoreGroups.Excellent].StudentCount);
        Assert.Equal(1, distributions[ScoreGroups.Good].StudentCount);
        Assert.Equal(1, distributions[ScoreGroups.Average].StudentCount);
        Assert.Equal(1, distributions[ScoreGroups.Weak].StudentCount);
        Assert.All(distributions.Values, d => Assert.Equal(0.25m, d.Percentage));

        var stats = await context.CourseStatistics.SingleAsync(v => v.CourseId == courseId);
        Assert.Equal(4, stats.GradedStudentCount);
        // AverageScore được tính lũy kế, làm tròn 2 chữ số sau mỗi lần chấm (không phải trung bình
        // đơn giản của 4 điểm): 9.0 → 8.00 → 7.00 → round((7.00*3 + 4.9)/4, 2) = round(6.475, 2) = 6.48
        // (MidpointRounding.ToEven mặc định của Math.Round: 6.47|5 làm tròn lên vì 7 là số lẻ).
        Assert.Equal(6.48m, stats.AverageScore);
    }
}
