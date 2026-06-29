using EnrollmentManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace EnrollmentManagement.Domain.Entities;

public class EnrolledSession : Entity {
    public Guid EnrollmentId { get; private set; }

    // FK trỏ sang ClassSession của CourseManagement BC (cross-BC reference by Id).
    public Guid ClassSessionId { get; private set; }

    public SessionType SessionType { get; private set; }

    // Required by EF Core.
    private EnrolledSession() { }

    internal static EnrolledSession Create(
        Guid enrollmentId,
        Guid classSessionId,
        SessionType sessionType) {
        return new EnrolledSession {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            ClassSessionId = classSessionId,
            SessionType = sessionType
        };
    }

    // Cập nhật khi Student điều chỉnh ca học (AdjustSession).
    internal void Adjust(Guid newClassSessionId, SessionType newSessionType) {
        ClassSessionId = newClassSessionId;
        SessionType = newSessionType;
    }
}