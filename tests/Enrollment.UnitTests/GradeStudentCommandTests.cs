using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Grading.GradeStudent;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentAggregate = EnrollmentManagement.Domain.Entities.Enrollment;

namespace Enrollment.UnitTests;

// Test precondition cross-aggregate của GradeStudentCommandHandler (Application layer):
// tồn tại → đúng chủ nhiệm → course gradeable → grade hợp lệ. Range của Grade tự nó thuộc
// Domain (GradeOutOfRange) nhưng handler là nơi ráp các precondition lại nên vẫn test qua đây.
public class GradeStudentCommandTests {
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LecturerId = Guid.NewGuid();
    private static readonly Guid SlotA = Guid.NewGuid();
    private static readonly Guid SlotB = Guid.NewGuid();

    private static readonly DateOnly Start = new(2026, 4, 1);
    private static readonly DateOnly End = new(2026, 6, 30);

    // Dựng handler quanh 1 enrollment cho sẵn, với course thuộc LecturerId và status cấu hình được.
    private static (GradeStudentCommandHandler Handler,
                    FakeEnrollmentRepository Repo,
                    FakeNotificationService Notifier,
                    FakeEnrollmentUnitOfWork Uow)
        Build(EnrollmentAggregate enrollment, string courseStatus = "Active", Guid? lecturerId = null) {
        var repo = new FakeEnrollmentRepository([enrollment]);
        var courseService = new FakeCourseEnrollmentService {
            DefaultStatus = courseStatus,
            Courses = [
                new CourseLookup(CourseId, "Test Course", lecturerId ?? LecturerId,
                    courseStatus, Start, End)
            ]
        };
        var notifier = new FakeNotificationService();
        var uow = new FakeEnrollmentUnitOfWork();

        var handler = new GradeStudentCommandHandler(repo, courseService, notifier, uow);
        return (handler, repo, notifier, uow);
    }

    private static EnrollmentAggregate NewEnrollment() =>
        EnrollmentTestData.CreateEnrollment(StudentId, CourseId, SlotA, SlotB);

    [Fact]
    public async Task Handle_EnrollmentNotFound_ReturnsNotFound() {
        var repo = new FakeEnrollmentRepository([]);
        var courseService = new FakeCourseEnrollmentService();
        var handler = new GradeStudentCommandHandler(
            repo, courseService, new FakeNotificationService(), new FakeEnrollmentUnitOfWork());

        var result = await handler.Handle(
            new GradeStudentCommand(Guid.NewGuid(), LecturerId, 8m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_LecturerNotCourseOwner_ReturnsNotCourseOwner() {
        var enrollment = NewEnrollment();
        var (handler, _, _, _) = Build(enrollment, lecturerId: Guid.NewGuid());

        // LecturerId trong command KHÁC lecturer phụ trách course.
        var result = await handler.Handle(
            new GradeStudentCommand(enrollment.Id, LecturerId, 8m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.NotCourseOwner, result.Error);
    }

    [Theory]
    [InlineData("Upcoming")]
    public async Task Handle_CourseNotActiveOrCompleted_ReturnsCourseNotGradeable(string status) {
        var enrollment = NewEnrollment();
        var (handler, _, _, _) = Build(enrollment, courseStatus: status);

        var result = await handler.Handle(
            new GradeStudentCommand(enrollment.Id, LecturerId, 8m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.CourseNotGradeable, result.Error);
    }

    [Fact]
    public async Task Handle_GradeOutOfRange_ReturnsGradeOutOfRange() {
        var enrollment = NewEnrollment();
        var (handler, _, _, uow) = Build(enrollment);

        var result = await handler.Handle(
            new GradeStudentCommand(enrollment.Id, LecturerId, 11m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.GradeOutOfRange, result.Error);
        Assert.Equal(0, uow.SaveChangesCallCount); // không commit khi grade sai
    }

    [Fact]
    public async Task Handle_ValidFirstTimeGrade_AssignsGrade_Commits_AndNotifies() {
        var enrollment = NewEnrollment();
        var (handler, _, notifier, uow) = Build(enrollment);

        var result = await handler.Handle(
            new GradeStudentCommand(enrollment.Id, LecturerId, 8.5m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(8.5m, result.Value.Grade);
        Assert.Equal(EnrollmentStatus.Graded, enrollment.Status);
        Assert.Equal(1, uow.SaveChangesCallCount);

        var notification = Assert.Single(notifier.Notifications);
        Assert.Equal(StudentId, notification.StudentId);
        Assert.Equal(8.5m, notification.Grade);
    }

    [Fact]
    public async Task Handle_RegradeAlreadyGraded_UpdatesGrade() {
        var enrollment = NewEnrollment();
        enrollment.AssignGrade(Grade.Create(6m).Value); // đã chấm lần đầu
        var (handler, _, _, uow) = Build(enrollment);

        var result = await handler.Handle(
            new GradeStudentCommand(enrollment.Id, LecturerId, 9m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(9m, result.Value.Grade);
        Assert.Equal(1, uow.SaveChangesCallCount);
    }
}
