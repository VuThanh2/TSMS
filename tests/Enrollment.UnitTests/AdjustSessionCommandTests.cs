using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Enrollments.AdjustSession;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Errors;

namespace Enrollment.UnitTests;

// Test AdjustSessionCommandHandler: đổi 1 WeeklySlot của enrollment sang slot khác cùng course,
// đồng thời đồng bộ Attendance các buổi TƯƠNG LAI (xóa buổi slot cũ, tạo buổi slot mới). Buổi đã
// qua giữ nguyên — nên các ClassSession dùng ngày tương lai để chắc chắn rơi vào nhánh "future".
public class AdjustSessionCommandTests {
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SlotMon = Guid.NewGuid();   // Monday Morning (slot cũ)
    private static readonly Guid SlotWed = Guid.NewGuid();   // Wednesday Afternoon (slot còn lại)
    private static readonly Guid SlotFri = Guid.NewGuid();   // Friday Morning (slot mới)

    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2030, 12, 31);
    private static readonly DateOnly FutureDate = new(2030, 6, 1);

    private sealed class Context {
        public FakeEnrollmentRepository EnrollmentRepo { get; }
        public FakeAttendanceRepository AttendanceRepo { get; }
        public FakeScheduleConflictChecker ConflictChecker { get; } =
            new FakeScheduleConflictChecker { Result = false };
        public FakeEnrollmentUnitOfWork Uow { get; } = new FakeEnrollmentUnitOfWork();
        public FakeCourseEnrollmentService CourseService { get; }
        public EnrollmentManagement.Domain.Entities.Enrollment Enrollment { get; }
        public Guid OldSessionId { get; } = Guid.NewGuid();

        public Context(string courseStatus = "Active") {
            Enrollment = EnrollmentTestData.CreateEnrollment(StudentId, CourseId, SlotMon, SlotWed);
            EnrollmentRepo = new FakeEnrollmentRepository([Enrollment]);

            // Đã có sẵn 1 Attendance cho buổi tương lai của slot cũ → phải bị xóa khi đổi slot.
            var oldAttendance = Attendance.CreateDefault(StudentId, OldSessionId, CourseId);
            AttendanceRepo = new FakeAttendanceRepository([oldAttendance]);

            CourseService = new FakeCourseEnrollmentService([
                new WeeklySlotLookup(SlotMon, CourseId, "Monday", "Morning"),
                new WeeklySlotLookup(SlotWed, CourseId, "Wednesday", "Afternoon"),
                new WeeklySlotLookup(SlotFri, CourseId, "Friday", "Morning")
            ]) {
                DefaultStatus = courseStatus,
                Courses = [new CourseLookup(CourseId, "Test Course", Guid.NewGuid(),
                    courseStatus, Start, End)],
                ClassSessions = [
                    new ClassSessionLookup(OldSessionId, CourseId, SlotMon, FutureDate, "Morning", false),
                    new ClassSessionLookup(Guid.NewGuid(), CourseId, SlotFri, FutureDate, "Morning", false)
                ]
            };
        }

        public AdjustSessionCommandHandler Handler() => new(
            EnrollmentRepo, AttendanceRepo, CourseService, ConflictChecker, Uow);
    }

    [Fact]
    public async Task Handle_EnrollmentBelongsToAnotherStudent_ReturnsNotFound() {
        var ctx = new Context();
        var command = new AdjustSessionCommand(
            ctx.Enrollment.Id, Guid.NewGuid(), SlotMon, SlotFri); // student khác

        var result = await ctx.Handler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_CourseCompleted_ReturnsCourseAlreadyCompleted() {
        var ctx = new Context(courseStatus: "Completed");
        var command = new AdjustSessionCommand(ctx.Enrollment.Id, StudentId, SlotMon, SlotFri);

        var result = await ctx.Handler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.CourseAlreadyCompleted, result.Error);
    }

    [Fact]
    public async Task Handle_NewSlotNotInCourse_ReturnsSessionNotInCourse() {
        var ctx = new Context();
        var command = new AdjustSessionCommand(ctx.Enrollment.Id, StudentId, SlotMon, Guid.NewGuid());

        var result = await ctx.Handler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.SessionNotInCourse, result.Error);
    }

    [Fact]
    public async Task Handle_ScheduleConflict_ReturnsScheduleConflict() {
        var ctx = new Context();
        ctx.ConflictChecker.Result = true;
        var command = new AdjustSessionCommand(ctx.Enrollment.Id, StudentId, SlotMon, SlotFri);

        var result = await ctx.Handler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.ScheduleConflict, result.Error);
    }

    [Fact]
    public async Task Handle_ValidAdjust_SyncsFutureAttendance_AndCommits() {
        var ctx = new Context();
        var command = new AdjustSessionCommand(ctx.Enrollment.Id, StudentId, SlotMon, SlotFri);

        var result = await ctx.Handler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Buổi tương lai của slot cũ bị xóa, buổi tương lai của slot mới được tạo.
        Assert.Single(ctx.AttendanceRepo.Removed);
        Assert.Single(ctx.AttendanceRepo.Added);
        Assert.Equal(1, ctx.Uow.SaveChangesCallCount);
    }
}
