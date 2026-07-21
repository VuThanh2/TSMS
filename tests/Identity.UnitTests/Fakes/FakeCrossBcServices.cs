using Identity.Application.Common.Interfaces;
using MediatR;

namespace Identity.UnitTests.Fakes;

// Fake ICourseLookupService (Identity→Course) — điều khiển 2 nhánh guard deactivate:
// Lecturer còn course active, và Student còn enroll trong course active.
public sealed class FakeCourseLookupService : ICourseLookupService {
    public bool LecturerHasActiveCourses { get; set; }
    public bool AnyCourseActive { get; set; }

    public Task<bool> HasActiveCoursesByLecturerAsync(
        Guid lecturerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(LecturerHasActiveCourses);

    public Task<bool> AreAnyActiveAsync(
        IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default) =>
        Task.FromResult(AnyCourseActive);
}

// Fake IEnrollmentIdentityService (Identity→Enrollment) — trả về danh sách courseId Student
// đang Active enroll, để test bước orchestrate 2 cross-BC call khi deactivate Student.
public sealed class FakeEnrollmentIdentityService : IEnrollmentIdentityService {
    public List<Guid> ActiveCourseIds { get; set; } = new();

    public Task<IReadOnlyList<Guid>> GetActiveCourseIdsByStudentAsync(
        Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(ActiveCourseIds);
}

// Fake IPublisher — ghi lại domain event đã publish (UserActivatedEvent/UserDeactivatedEvent)
// để test khẳng định handler bắn đúng event sau khi đổi trạng thái thành công.
public sealed class FakePublisher : IPublisher {
    public List<object> Published { get; } = new();

    public Task Publish(object notification, CancellationToken cancellationToken = default) {
        Published.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(
        TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification {
        Published.Add(notification!);
        return Task.CompletedTask;
    }
}
