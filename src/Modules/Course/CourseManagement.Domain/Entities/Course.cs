using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Events;
using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Domain.Entities;

public class Course : AggregateRoot {
    // EF Core backing fields for Value Objects
    private string _courseName = string.Empty;
    private DateOnly _startDate;
    private DateOnly _endDate;

    private readonly List<ClassSession> _classSessions = [];

    public Guid LecturerId { get; private set; }
    public string Name => _courseName;
    public string? Description { get; private set; }
    public DateOnly StartDate => _startDate;
    public DateOnly EndDate => _endDate;
    public CourseStatus Status { get; private set; }
    public int MaxCapacity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<ClassSession> ClassSessions => _classSessions.AsReadOnly();

    // Required by EF Core.
    private Course() { }

    /// Creates a new Course in Upcoming status.
    public static Result<Course> Create(
        Guid lecturerId,
        CourseName courseName,
        string? description,
        DateRange dateRange,
        int maxCapacity) {
        if (maxCapacity <= 0)
            return Result.Failure<Course>(CourseErrors.MaxCapacityMustBePositive);

        var course = new Course {
            Id = Guid.NewGuid(),
            LecturerId = lecturerId,
            _courseName = courseName.Value,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            _startDate = dateRange.StartDate,
            _endDate = dateRange.EndDate,
            Status = CourseStatus.Upcoming,
            MaxCapacity = maxCapacity,
            CreatedAt = DateTime.UtcNow
        };

        course.RaiseDomainEvent(CourseCreatedEvent.Create(
            course.Id, course.LecturerId, course._courseName,
            course._startDate, course._endDate, course.MaxCapacity));

        return Result.Success(course);
    }

    // ── Behaviour methods

    /// Application Layer must check maxCapacity >= currentEnrollmentCount before calling.
    public Result UpdateInfo(
        CourseName newCourseName,
        string? newDescription,
        DateOnly newEndDate,
        int newMaxCapacity) {
        if (Status == CourseStatus.Completed)
            return Result.Failure(CourseErrors.CompletedCourseIsImmutable);

        if (newMaxCapacity <= 0)
            return Result.Failure(CourseErrors.MaxCapacityMustBePositive);

        // EndDate must not precede the latest ClassSession date.
        var latestSession = _classSessions
            .OrderByDescending(s => s.SessionDate)
            .FirstOrDefault();

        if (latestSession is not null && newEndDate < latestSession.SessionDate)
            return Result.Failure(CourseErrors.EndDatePrecedesExistingClassSession);

        var dateRangeResult = DateRange.WithNewEndDate(_startDate, newEndDate);
        if (dateRangeResult.IsFailure)
            return Result.Failure(dateRangeResult.Error);

        _courseName = newCourseName.Value;
        Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
        _endDate = dateRangeResult.Value.EndDate;
        MaxCapacity = newMaxCapacity;

        RaiseDomainEvent(CourseUpdatedEvent.Create(Id, _courseName, _endDate, MaxCapacity));

        return Result.Success();
    }

    /// Pre-conditions the Application Layer must guarantee:
    ///   - newLecturerId refers to an Active Lecturer.
    ///   - newLecturerId != current LecturerId.
    ///   - No date range overlap with this Lecturer's other courses.
    public Result ReplaceLecturer(Guid newLecturerId) {
        if (Status == CourseStatus.Completed)
            return Result.Failure(CourseErrors.CompletedCourseIsImmutable);
        
        if (newLecturerId == LecturerId)
            return Result.Failure(CourseErrors.LecturerAlreadyAssigned);

        var previousLecturerId = LecturerId;
        LecturerId = newLecturerId;

        RaiseDomainEvent(LecturerReplacedEvent.Create(Id, previousLecturerId, newLecturerId));

        return Result.Success();
    }
    
    /// Called exclusively by the Background Job
    public Result TransitionStatus(CourseStatus targetStatus) {
        var isValid = (Status, targetStatus) switch {
            (CourseStatus.Upcoming, CourseStatus.Active)    => true,
            (CourseStatus.Active,   CourseStatus.Completed) => true,
            _                                               => false
        };

        if (!isValid)
            return Result.Failure(CourseErrors.InvalidStatusTransition);

        Status = targetStatus;

        RaiseDomainEvent(CourseStatusChangedEvent.Create(Id, targetStatus));

        return Result.Success();
    }

    public Result<ClassSession> AddClassSession(DateOnly sessionDate, SessionType sessionType) {
        if (Status == CourseStatus.Completed)
            return Result.Failure<ClassSession>(CourseErrors.CompletedCourseIsImmutable);

        if (sessionDate < _startDate || sessionDate > _endDate)
            return Result.Failure<ClassSession>(CourseErrors.ClassSessionOutsideDateRange);

        var hasDuplicate = _classSessions.Any(s =>
            s.SessionDate == sessionDate && s.SessionType == sessionType);

        if (hasDuplicate)
            return Result.Failure<ClassSession>(CourseErrors.DuplicateClassSession);

        var session = ClassSession.Create(Id, sessionDate, sessionType);
        _classSessions.Add(session);

        RaiseDomainEvent(ClassSessionAddedEvent.Create(Id, session.Id, sessionDate, sessionType));

        return Result.Success(session);
    }

    public Result UpdateClassSession(
        Guid classSessionId,
        DateOnly newSessionDate,
        SessionType newSessionType) {
        if (Status == CourseStatus.Completed)
            return Result.Failure(CourseErrors.CompletedCourseIsImmutable);

        var session = _classSessions.FirstOrDefault(s => s.Id == classSessionId);
        if (session is null)
            return Result.Failure(CourseErrors.ClassSessionNotFound);

        if (session.IsPast())
            return Result.Failure(CourseErrors.CannotModifyPastClassSession);

        if (newSessionDate < _startDate || newSessionDate > _endDate)
            return Result.Failure(CourseErrors.ClassSessionOutsideDateRange);

        var hasDuplicate = _classSessions.Any(s =>
            s.Id != classSessionId &&
            s.SessionDate == newSessionDate &&
            s.SessionType == newSessionType);

        if (hasDuplicate)
            return Result.Failure(CourseErrors.DuplicateClassSession);

        var updateResult = session.Update(newSessionDate, newSessionType);
        if (updateResult.IsFailure)
            return updateResult;

        RaiseDomainEvent(ClassSessionUpdatedEvent.Create(Id, session.Id, newSessionDate, newSessionType));

        return Result.Success();
    }
    
    /// Enforces: session exists, not past, minimum 2 sessions must remain.
    public Result RemoveClassSession(Guid classSessionId) {
        var session = _classSessions.FirstOrDefault(s => s.Id == classSessionId);
        if (session is null)
            return Result.Failure(CourseErrors.ClassSessionNotFound);

        if (session.IsPast())
            return Result.Failure(CourseErrors.CannotModifyPastClassSession);

        if (_classSessions.Count <= 2)
            return Result.Failure(CourseErrors.MinimumClassSessionsRequired);

        _classSessions.Remove(session);

        RaiseDomainEvent(ClassSessionRemovedEvent.Create(Id, classSessionId));

        return Result.Success();
    }
}