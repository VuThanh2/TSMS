using Identity.Application.Users.UpdateUserStatus;
using Identity.Domain.Entities;
using Identity.Domain.Errors;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;
using Identity.UnitTests.Fakes;

namespace Identity.UnitTests;

// Test UpdateUserStatusCommandHandler — nơi tập trung precondition cross-BC nhiều nhất của Identity:
//   Deactivate: không tự vô hiệu hóa mình; Lecturer không còn course active; Student không còn
//   enroll trong course active. Các nhánh fail đều return TRƯỚC khi chạm UserManager, nên chỉ
//   nhánh success mới thực sự gọi UpdateAsync/SetLockoutEndDateAsync (TestUserManager ghi lại).
public class UpdateUserStatusCommandTests {
    private static AppUser CreateUser(UserRole role, Guid? id = null) {
        var user = AppUser.Create(
            id ?? Guid.NewGuid(),
            $"{Guid.NewGuid():N}@example.com",
            "Nguyen Van A",
            role).Value;
        user.ClearDomainEvents(); // bỏ UserCreatedEvent để chỉ assert event của lần đổi trạng thái
        return user;
    }

    private static (UpdateUserStatusCommandHandler Handler,
                    TestUserManager UserManager,
                    FakePublisher Publisher)
        Build(AppUser? user,
              bool lecturerHasActiveCourses = false,
              bool anyCourseActive = false,
              List<Guid>? activeCourseIds = null) {
        var repo = new FakeUserRepository(user is null ? [] : [user]);
        var userManager = new TestUserManager();
        var courseLookup = new FakeCourseLookupService {
            LecturerHasActiveCourses = lecturerHasActiveCourses,
            AnyCourseActive = anyCourseActive
        };
        var enrollmentIdentity = new FakeEnrollmentIdentityService {
            ActiveCourseIds = activeCourseIds ?? new List<Guid>()
        };
        var publisher = new FakePublisher();

        var handler = new UpdateUserStatusCommandHandler(
            repo, userManager, courseLookup, enrollmentIdentity, publisher);
        return (handler, userManager, publisher);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound() {
        var (handler, _, _) = Build(user: null);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(Guid.NewGuid(), false, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_AdminDeactivatesSelf_ReturnsCannotDeactivateSelf() {
        var admin = CreateUser(UserRole.Admin);
        var (handler, userManager, _) = Build(admin);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(admin.Id, false, admin.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.CannotDeactivateSelf, result.Error);
        Assert.Equal(0, userManager.UpdateCallCount); // fail trước khi chạm UserManager
    }

    [Fact]
    public async Task Handle_LecturerWithActiveCourses_ReturnsLecturerHasActiveCourses() {
        var lecturer = CreateUser(UserRole.Lecturer);
        var (handler, _, _) = Build(lecturer, lecturerHasActiveCourses: true);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(lecturer.Id, false, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.LecturerHasActiveCourses, result.Error);
    }

    [Fact]
    public async Task Handle_StudentWithActiveEnrollments_ReturnsStudentHasActiveEnrollments() {
        var student = CreateUser(UserRole.Student);
        var (handler, _, _) = Build(
            student, anyCourseActive: true, activeCourseIds: [Guid.NewGuid()]);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(student.Id, false, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.StudentHasActiveEnrollments, result.Error);
    }

    [Fact]
    public async Task Handle_DeactivateStudentWithoutActiveCourses_Succeeds_SetsLockout_PublishesEvent() {
        var student = CreateUser(UserRole.Student); // mặc định Active

        var (handler, userManager, publisher) = Build(student);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(student.Id, false, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.False(student.IsActive);
        Assert.Equal(1, userManager.UpdateCallCount);
        Assert.True(userManager.LockoutSet);
        Assert.Equal(DateTimeOffset.MaxValue, userManager.LastLockoutEnd); // khóa login
        Assert.Single(publisher.Published);
        Assert.IsType<UserDeactivatedEvent>(publisher.Published[0]);
    }

    [Fact]
    public async Task Handle_ActivateInactiveUser_Succeeds_ClearsLockout_PublishesEvent() {
        var student = CreateUser(UserRole.Student);
        student.Deactivate();       // đưa về inactive
        student.ClearDomainEvents(); // bỏ event deactivate để chỉ còn assert event activate

        var (handler, userManager, publisher) = Build(student);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(student.Id, true, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.True(student.IsActive);
        Assert.Equal(1, userManager.UpdateCallCount);
        Assert.Null(userManager.LastLockoutEnd); // mở khóa login
        Assert.Single(publisher.Published);
        Assert.IsType<UserActivatedEvent>(publisher.Published[0]);
    }
}
