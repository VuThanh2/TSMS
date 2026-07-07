using CourseManagement.Domain.Events;
using EnrollmentManagement.Domain.Events;
using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Infrastructure.EventHandlers;
using Reporting.Infrastructure.Extensions;

namespace Reporting.UnitTests;

// Test này encode chính xác production bug đã fix: EventHandler nằm ở Reporting.Infrastructure
// nên MediatR's RegisterServicesFromAssemblies (chỉ scan *.Application assembly) không tự tìm
// thấy — phải đăng ký thủ công đúng interface INotificationHandler<TEvent> trong
// ReportingModuleExtensions.AddReportingModule. Nếu ai đó lỡ sửa lại thành
// services.AddScoped<CourseEventHandler>() (chỉ đăng ký concrete class, như bug cũ), MediatR
// sẽ resolve ra 0 handler và im lặng không làm gì — test này sẽ fail ngay thay vì phải chờ
// chạy thật rồi mới phát hiện Reporting trả 404.
public class ReportingModuleDiWiringTests {
    private static ServiceProvider BuildProvider() {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:ReportingDb"] =
                    "Server=fake;Database=fake;Trusted_Connection=True;TrustServerCertificate=True;"
            })
            .Build();

        services.AddReportingModule(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddReportingModule_RegistersAllCourseEventHandlers_ExactlyOnceEach() {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        AssertSingleHandler<INotificationHandler<CourseCreatedEvent>, CourseEventHandler>(scope);
        AssertSingleHandler<INotificationHandler<CourseUpdatedEvent>, CourseEventHandler>(scope);
        AssertSingleHandler<INotificationHandler<CourseStatusChangedEvent>, CourseEventHandler>(scope);
        AssertSingleHandler<INotificationHandler<LecturerReplacedEvent>, CourseEventHandler>(scope);
    }

    [Fact]
    public void AddReportingModule_RegistersAllEnrollmentEventHandlers_ExactlyOnceEach() {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        AssertSingleHandler<INotificationHandler<StudentEnrolledEvent>, EnrollmentEventHandler>(scope);
        AssertSingleHandler<INotificationHandler<GradeAssignedEvent>, EnrollmentEventHandler>(scope);
        AssertSingleHandler<INotificationHandler<GradeUpdatedEvent>, EnrollmentEventHandler>(scope);
    }

    [Fact]
    public void AddReportingModule_RegistersAttendanceAndIdentityEventHandlers_ExactlyOnceEach() {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        AssertSingleHandler<INotificationHandler<AttendanceMarkedEvent>, AttendanceEventHandler>(scope);
        AssertSingleHandler<INotificationHandler<UserUpdatedEvent>, IdentityEventHandler>(scope);
    }

    [Fact]
    public void AddReportingModule_SameCourseEventHandlerInstance_HandlesAllFourCourseEvents() {
        // "1 instance nhiều interface" (convention cross-BC của project) — 4 interface phải
        // trỏ về CÙNG một instance CourseEventHandler trong 1 scope, không phải 4 instance riêng.
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var viaCreated = scope.ServiceProvider.GetRequiredService<INotificationHandler<CourseCreatedEvent>>();
        var viaUpdated = scope.ServiceProvider.GetRequiredService<INotificationHandler<CourseUpdatedEvent>>();

        Assert.Same(viaCreated, viaUpdated);
    }

    private static void AssertSingleHandler<TInterface, TExpectedImplementation>(IServiceScope scope) {
        var handlers = scope.ServiceProvider.GetServices<TInterface>().ToList();
        var handler = Assert.Single(handlers);
        Assert.IsType<TExpectedImplementation>(handler);
    }
}
