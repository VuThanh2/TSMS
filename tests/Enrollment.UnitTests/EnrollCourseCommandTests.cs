using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Enrollments.EnrollCourse;
using EnrollmentManagement.Domain.Errors;

namespace Enrollment.UnitTests;

// Test chuỗi precondition của EnrollCourseCommandHandler — đây là use case có nhiều guard nhất
// (student active → course upcoming → cổng mở → chưa đăng ký → còn chỗ → slot thuộc course →
// không trùng lịch). Mỗi test giữ nguyên "happy path" rồi bẻ ĐÚNG 1 điều kiện để cô lập nhánh lỗi.
public class EnrollCourseCommandTests {
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SlotMon = Guid.NewGuid();
    private static readonly Guid SlotWed = Guid.NewGuid();

    private static readonly DateOnly Start = new(2026, 4, 1);
    private static readonly DateOnly End = new(2026, 6, 30);

    // Bối cảnh mặc định: mọi precondition đều thông. Test chỉ chỉnh field cần thiết.
    private sealed class Context {
        public FakeEnrollmentRepository EnrollmentRepo { get; } =
            new FakeEnrollmentRepository([]);
        public FakeAttendanceRepository AttendanceRepo { get; } =
            new FakeAttendanceRepository();
        public FakeStudentEnrollmentService StudentService { get; } =
            new FakeStudentEnrollmentService();
        public FakeScheduleConflictChecker ConflictChecker { get; } =
            new FakeScheduleConflictChecker { Result = false };
        public FakeEnrollmentUnitOfWork Uow { get; } = new FakeEnrollmentUnitOfWork();
        public FakeCourseEnrollmentService CourseService { get; }

        public Context() {
            CourseService = new FakeCourseEnrollmentService([
                new WeeklySlotLookup(SlotMon, CourseId, "Monday", "Morning"),
                new WeeklySlotLookup(SlotWed, CourseId, "Wednesday", "Afternoon")
            ]) {
                Upcoming = true,
                OpenForEnrollment = true,
                DefaultStatus = "Upcoming",
                Courses = [new CourseLookup(CourseId, "Test Course", Guid.NewGuid(),
                    "Upcoming", Start, End)],
                // 2 buổi cho mỗi slot → attendance được sinh cho đúng các buổi này.
                ClassSessions = [
                    new ClassSessionLookup(Guid.NewGuid(), CourseId, SlotMon,
                        new DateOnly(2026, 4, 6), "Morning", false),
                    new ClassSessionLookup(Guid.NewGuid(), CourseId, SlotWed,
                        new DateOnly(2026, 4, 8), "Afternoon", false)
                ]
            };
        }

        public EnrollCourseCommandHandler Handler() => new(
            EnrollmentRepo, AttendanceRepo, CourseService, StudentService, ConflictChecker, Uow);

        public EnrollCourseCommand Command() =>
            new(StudentId, CourseId, [SlotMon, SlotWed]);
    }

    [Fact]
    public async Task Handle_StudentNotActive_ReturnsStudentNotActive() {
        var ctx = new Context();
        ctx.StudentService.ActiveStudent = false;

        var result = await ctx.Handler().Handle(ctx.Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.StudentNotActive, result.Error);
    }

    [Fact]
    public async Task Handle_CourseNotUpcoming_ReturnsCourseNotEnrollable() {
        var ctx = new Context();
        ctx.CourseService.Upcoming = false;

        var result = await ctx.Handler().Handle(ctx.Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.CourseNotEnrollable, result.Error);
    }

    [Fact]
    public async Task Handle_CourseNotOpen_ReturnsCourseNotOpenForEnrollment() {
        var ctx = new Context();
        ctx.CourseService.OpenForEnrollment = false;

        var result = await ctx.Handler().Handle(ctx.Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.CourseNotOpenForEnrollment, result.Error);
    }

    [Fact]
    public async Task Handle_AlreadyEnrolled_ReturnsAlreadyEnrolled() {
        var ctx = new Context();
        // Đã có enrollment của chính student này trong course này.
        var existing = EnrollmentTestData.CreateEnrollment(StudentId, CourseId, SlotMon, SlotWed);
        var handler = new EnrollCourseCommandHandler(
            new FakeEnrollmentRepository([existing]), ctx.AttendanceRepo, ctx.CourseService,
            ctx.StudentService, ctx.ConflictChecker, ctx.Uow);

        var result = await handler.Handle(ctx.Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.AlreadyEnrolled, result.Error);
    }

    [Fact]
    public async Task Handle_CourseAtMaxCapacity_ReturnsCourseIsFull() {
        var ctx = new Context();
        ctx.CourseService.MaxCapacity = 0; // sức chứa 0 → luôn đầy

        var result = await ctx.Handler().Handle(ctx.Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.CourseIsFull, result.Error);
    }

    [Fact]
    public async Task Handle_SlotNotInCourse_ReturnsSessionNotInCourse() {
        var ctx = new Context();
        var command = new EnrollCourseCommand(StudentId, CourseId, [SlotMon, Guid.NewGuid()]);

        var result = await ctx.Handler().Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.SessionNotInCourse, result.Error);
    }

    [Fact]
    public async Task Handle_ScheduleConflict_ReturnsScheduleConflict() {
        var ctx = new Context();
        ctx.ConflictChecker.Result = true;

        var result = await ctx.Handler().Handle(ctx.Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.ScheduleConflict, result.Error);
    }

    [Fact]
    public async Task Handle_AllPreconditionsPass_CreatesEnrollment_GeneratesAttendance_Commits() {
        var ctx = new Context();

        var result = await ctx.Handler().Handle(ctx.Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(ctx.EnrollmentRepo.Added);
        // Attendance sinh đúng cho 2 buổi của 2 slot đã chọn — không phải toàn bộ course.
        Assert.Equal(2, ctx.AttendanceRepo.Added.Count);
        Assert.Equal(1, ctx.Uow.SaveChangesCallCount);
    }
}
