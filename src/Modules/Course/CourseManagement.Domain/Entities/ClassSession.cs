using CourseManagement.Domain.Errors;
using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Domain.Entities;

public class ClassSession : Entity {
    public Guid CourseId { get; private set; }
    public DateOnly SessionDate { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public SessionType SessionType { get; private set; }

    // Required by EF Core.
    private ClassSession() { }

    internal static ClassSession Create(
        Guid courseId,
        DateOnly sessionDate,
        SessionType sessionType) {
        return new ClassSession {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            SessionDate = sessionDate,
            DayOfWeek = sessionDate.DayOfWeek,
            SessionType = sessionType
        };
    }

    /// Returns true if this session has already passed relative to today.
    /// Convention: today's session is NOT considered past (date-only comparison).
    public bool IsPast() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return SessionDate < today;
    }
    
    /// Caller must validate: not past, no duplicate in course, within course date range.
    internal Result Update(DateOnly newSessionDate, SessionType newSessionType) {
        SessionDate = newSessionDate;
        DayOfWeek = newSessionDate.DayOfWeek;
        SessionType = newSessionType;

        return Result.Success();
    }
}