using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Application.Attendances.MarkAttendance;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.ValueObjects;

namespace Enrollment.UnitTests;

// Test precondition của MarkAttendanceCommandHandler: attendance tồn tại → đúng chủ nhiệm →
// buổi không bị hủy → status parse được. Riêng status không hợp lệ được ánh xạ về AttendanceNotFound
// (quyết định của handler), nên test bám đúng error đó chứ không tự suy ra error khác.
public class MarkAttendanceCommandTests {
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LecturerId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.NewGuid();

    private static readonly DateOnly Start = new(2026, 4, 1);
    private static readonly DateOnly End = new(2026, 6, 30);

    private static Attendance NewAttendance() =>
        Attendance.CreateDefault(StudentId, SessionId, CourseId);

    private static (MarkAttendanceCommandHandler Handler, FakeEnrollmentUnitOfWork Uow)
        Build(Attendance? attendance, Guid? lecturerId = null,
              IEnumerable<ClassSessionLookup>? classSessions = null) {
        var repo = new FakeAttendanceRepository(attendance is null ? [] : [attendance]);
        var courseService = new FakeCourseEnrollmentService {
            Courses = [
                new CourseLookup(CourseId, "Test Course", lecturerId ?? LecturerId,
                    "Active", Start, End)
            ],
            ClassSessions = classSessions?.ToList() ?? new List<ClassSessionLookup>()
        };
        var uow = new FakeEnrollmentUnitOfWork();
        return (new MarkAttendanceCommandHandler(repo, courseService, uow), uow);
    }

    [Fact]
    public async Task Handle_AttendanceNotFound_ReturnsAttendanceNotFound() {
        var (handler, _) = Build(attendance: null);

        var result = await handler.Handle(
            new MarkAttendanceCommand(Guid.NewGuid(), LecturerId, "Present"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.AttendanceNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_LecturerNotCourseOwner_ReturnsNotCourseOwner() {
        var attendance = NewAttendance();
        var (handler, _) = Build(attendance, lecturerId: Guid.NewGuid());

        var result = await handler.Handle(
            new MarkAttendanceCommand(attendance.Id, LecturerId, "Present"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.NotCourseOwner, result.Error);
    }

    [Fact]
    public async Task Handle_SessionCancelled_ReturnsSessionCancelled() {
        var attendance = NewAttendance();
        var (handler, _) = Build(attendance, classSessions: [
            new ClassSessionLookup(SessionId, CourseId, Guid.NewGuid(),
                new DateOnly(2026, 5, 1), "Morning", IsCancelled: true)
        ]);

        var result = await handler.Handle(
            new MarkAttendanceCommand(attendance.Id, LecturerId, "Present"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.SessionCancelled, result.Error);
    }

    [Fact]
    public async Task Handle_InvalidStatusString_ReturnsAttendanceNotFound() {
        var attendance = NewAttendance();
        var (handler, _) = Build(attendance);

        var result = await handler.Handle(
            new MarkAttendanceCommand(attendance.Id, LecturerId, "Sleeping"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.AttendanceNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ValidMark_UpdatesStatus_AndCommits() {
        var attendance = NewAttendance(); // mặc định Absent
        var (handler, uow) = Build(attendance);

        var result = await handler.Handle(
            new MarkAttendanceCommand(attendance.Id, LecturerId, "present"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AttendanceStatus.Present, attendance.Status);
        Assert.Equal("Present", result.Value.AttendanceStatus);
        Assert.Equal(1, uow.SaveChangesCallCount);
    }
}
