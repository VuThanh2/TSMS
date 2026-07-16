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

    private readonly List<WeeklySlot> _weeklySlots = [];
    private readonly List<ClassSession> _classSessions = [];

    public Guid LecturerId { get; private set; }
    public string Name => _courseName;
    public string? Description { get; private set; }
    public DateOnly StartDate => _startDate;
    public DateOnly EndDate => _endDate;
    public CourseStatus Status { get; private set; }
    public int MaxCapacity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Cổng đăng ký, TÁCH BIỆT với Status. Course mới tạo = false → Student không thấy, không
    // enroll được, nên Admin có cửa sổ an toàn để dựng lịch và xóa nếu tạo sai. Không dùng
    // thêm giá trị CourseStatus vì Status do Hangfire lái theo ngày tháng, còn cổng này do
    // Admin bấm — 2 trục độc lập, gộp vào 1 enum sẽ phải sửa cả state machine lẫn job.
    public bool IsOpenForEnrollment { get; private set; }

    public IReadOnlyList<WeeklySlot> WeeklySlots => _weeklySlots.AsReadOnly();
    public IReadOnlyList<ClassSession> ClassSessions => _classSessions.AsReadOnly();

    // Required by EF Core.
    private Course() { }

    /// Creates a new Course in Upcoming status. Chưa có WeeklySlot nào — Admin phải
    /// gọi AddWeeklySlot() tối thiểu 2 lần sau khi tạo (rule: tối thiểu 2 ca/tuần).
    public static Result<Course> Create(
        Guid lecturerId,
        CourseName courseName,
        string? description,
        DateRange dateRange,
        int maxCapacity,
        string lecturerName) {
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
            // Course sinh ra ở trạng thái ĐÓNG — Admin phải chủ động mở sau khi dựng xong lịch.
            IsOpenForEnrollment = false,
            CreatedAt = DateTime.UtcNow
        };

        course.RaiseDomainEvent(CourseCreatedEvent.Create(
            course.Id, course.LecturerId, course._courseName, lecturerName,
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

        // EndDate mới không được đứng trước buổi học ĐÃ QUA gần nhất — bảo toàn lịch sử điểm danh.
        // (Buổi tương lai vượt EndDate mới sẽ tự động bị dọn ở RegenerateSessionsForNewEndDate.)
        var latestPastSession = _classSessions
            .Where(s => s.IsPast())
            .OrderByDescending(s => s.SessionDate)
            .FirstOrDefault();

        if (latestPastSession is not null && newEndDate < latestPastSession.SessionDate)
            return Result.Failure(CourseErrors.EndDatePrecedesExistingClassSession);

        var dateRangeResult = DateRange.WithNewEndDate(_startDate, newEndDate);
        if (dateRangeResult.IsFailure)
            return Result.Failure(dateRangeResult.Error);

        var oldEndDate = _endDate;

        _courseName = newCourseName.Value;
        Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
        _endDate = dateRangeResult.Value.EndDate;
        MaxCapacity = newMaxCapacity;

        RegenerateSessionsForNewEndDate(oldEndDate, _endDate);

        RaiseDomainEvent(CourseUpdatedEvent.Create(Id, _courseName, _endDate, MaxCapacity));

        return Result.Success();
    }

    /// Pre-conditions the Application Layer must guarantee:
    ///   - newLecturerId refers to an Active Lecturer.
    ///   - newLecturerId != current LecturerId.
    ///   - No date range overlap with this Lecturer's other courses.
    public Result ReplaceLecturer(Guid newLecturerId, string newLecturerName) {
        if (Status == CourseStatus.Completed)
            return Result.Failure(CourseErrors.CompletedCourseIsImmutable);

        if (newLecturerId == LecturerId)
            return Result.Failure(CourseErrors.LecturerAlreadyAssigned);

        var previousLecturerId = LecturerId;
        LecturerId = newLecturerId;

        RaiseDomainEvent(LecturerReplacedEvent.Create(Id, previousLecturerId, newLecturerId, newLecturerName));

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

    /// Mở cổng đăng ký cho Student. Invariant:
    ///   - Chỉ Course Upcoming mới mở được — Active/Completed đã qua kỳ đăng ký.
    ///   - Phải có tối thiểu 2 WeeklySlot: Student bắt buộc chọn đúng 2 ca khi enroll
    ///     (Enrollment.Create), mở khi chưa đủ slot sẽ đẩy họ vào màn hình chọn ca bất khả thi.
    /// Idempotent: mở lại Course đã mở không phải lỗi, chỉ là no-op.
    public Result OpenEnrollment() {
        if (Status != CourseStatus.Upcoming)
            return Result.Failure(CourseErrors.OnlyUpcomingCourseCanOpenEnrollment);

        if (_weeklySlots.Count < 2)
            return Result.Failure(CourseErrors.MinimumWeeklySlotsRequiredToOpen);

        IsOpenForEnrollment = true;

        return Result.Success();
    }

    /// Xóa Course. Domain invariant: chỉ được xóa khi còn Upcoming — course đã Active/Completed
    /// đã phát sinh buổi học đã diễn ra / lịch sử nên không được xóa. Precondition "không có
    /// Student enroll" thuộc Application layer (cross-BC). Raise CourseDeletedEvent để Reporting
    /// dọn projection tương ứng. Việc remove khỏi DB do repository thực hiện ở handler.
    public Result Delete() {
        if (Status != CourseStatus.Upcoming)
            return Result.Failure(CourseErrors.OnlyUpcomingCourseCanBeDeleted);

        RaiseDomainEvent(CourseDeletedEvent.Create(Id));

        return Result.Success();
    }

    // ── WeeklySlot behaviour

    /// Tạo 1 WeeklySlot mới và SINH TOÀN BỘ ClassSession tương ứng từ StartDate đến EndDate
    /// của Course. Đây là cách duy nhất để thêm buổi học vào Course.
    public Result<WeeklySlot> AddWeeklySlot(DayOfWeek dayOfWeek, SessionType sessionType) {
        if (Status == CourseStatus.Completed)
            return Result.Failure<WeeklySlot>(CourseErrors.CompletedCourseIsImmutable);

        var hasDuplicate = _weeklySlots.Any(s => s.DayOfWeek == dayOfWeek && s.SessionType == sessionType);
        if (hasDuplicate)
            return Result.Failure<WeeklySlot>(CourseErrors.DuplicateWeeklySlot);

        var slot = WeeklySlot.Create(Id, dayOfWeek, sessionType);
        _weeklySlots.Add(slot);

        var generatedSessions = GenerateClassSessions(slot, _startDate, _endDate);
        _classSessions.AddRange(generatedSessions);

        RaiseDomainEvent(WeeklySlotAddedEvent.Create(
            Id, slot.Id, dayOfWeek, sessionType, generatedSessions.Count));

        return Result.Success(slot);
    }

    /// Xóa 1 WeeklySlot. XÓA HẲN các ClassSession TƯƠNG LAI của slot này — buổi đã qua
    /// giữ nguyên để bảo toàn lịch sử điểm danh. Enforces tối thiểu 2 WeeklySlot phải còn lại.
    /// Pre-condition Application Layer phải đảm bảo: không còn Enrollment nào đang dùng slot này
    /// (cross-BC check qua IEnrollmentCourseService.IsWeeklySlotInUseAsync).
    public Result RemoveWeeklySlot(Guid weeklySlotId) {
        if (Status == CourseStatus.Completed)
            return Result.Failure(CourseErrors.CompletedCourseIsImmutable);

        var slot = _weeklySlots.FirstOrDefault(s => s.Id == weeklySlotId);
        if (slot is null)
            return Result.Failure(CourseErrors.WeeklySlotNotFound);

        if (_weeklySlots.Count <= 2)
            return Result.Failure(CourseErrors.MinimumWeeklySlotsRequired);

        var futureSessions = _classSessions
            .Where(s => s.WeeklySlotId == weeklySlotId && !s.IsPast())
            .ToList();

        foreach (var session in futureSessions)
            _classSessions.Remove(session);

        _weeklySlots.Remove(slot);

        RaiseDomainEvent(WeeklySlotRemovedEvent.Create(
            Id, weeklySlotId, futureSessions.Select(s => s.Id).ToList()));

        return Result.Success();
    }

    // ── Single ClassSession behaviour (override 1 buổi cụ thể, vd nghỉ lễ dời lịch)

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

    /// Hủy 1 buổi CỤ THỂ (vd nghỉ lễ) mà không xóa cả WeeklySlot — các tuần khác vẫn diễn ra bình thường.
    /// Không còn enforce "tối thiểu 2 ClassSession" — rule đó giờ thuộc về WeeklySlot (RemoveWeeklySlot).
    public Result CancelClassSession(Guid classSessionId) {
        if (Status == CourseStatus.Completed)
            return Result.Failure(CourseErrors.CompletedCourseIsImmutable);

        var session = _classSessions.FirstOrDefault(s => s.Id == classSessionId);
        if (session is null)
            return Result.Failure(CourseErrors.ClassSessionNotFound);

        if (session.IsPast())
            return Result.Failure(CourseErrors.CannotModifyPastClassSession);

        if (session.IsCancelled)
            return Result.Failure(CourseErrors.ClassSessionAlreadyCancelled);

        var cancelResult = session.Cancel();
        if (cancelResult.IsFailure)
            return cancelResult;

        RaiseDomainEvent(ClassSessionCancelledEvent.Create(Id, classSessionId));

        return Result.Success();
    }

    // ── Private helpers

    /// Sinh danh sách ClassSession cho 1 WeeklySlot, trải từ fromDate đến toDate (inclusive),
    /// mỗi 7 ngày 1 buổi đúng theo DayOfWeek của slot.
    ///
    /// BỎ QUA ngày đã có ClassSession cùng (SessionDate, SessionType) — invariant "1 Course không
    /// có 2 buổi trùng ngày + ca" (unique index dưới DB). Các buổi đã chiếm chỗ là buổi LỊCH SỬ
    /// không thể xóa: buổi đã qua của slot vừa bị xóa, hoặc buổi bị soft-cancel khi rút ngắn
    /// EndDate. Không bỏ qua thì thêm lại đúng slot cũ / gia hạn lại EndDate sẽ vỡ ở DB.
    private List<ClassSession> GenerateClassSessions(WeeklySlot slot, DateOnly fromDate, DateOnly toDate) {
        var sessions = new List<ClassSession>();

        if (fromDate > toDate)
            return sessions;

        var occupiedDates = _classSessions
            .Where(s => s.SessionType == slot.SessionType)
            .Select(s => s.SessionDate)
            .ToHashSet();

        var current = fromDate;
        while (current.DayOfWeek != slot.DayOfWeek)
            current = current.AddDays(1);

        while (current <= toDate) {
            if (!occupiedDates.Contains(current))
                sessions.Add(ClassSession.Create(slot.CourseId, slot.Id, current, slot.SessionType));

            current = current.AddDays(7);
        }

        return sessions;
    }

    /// Đồng bộ ClassSession khi EndDate thay đổi:
    ///   - Rút ngắn: HỦY (soft-cancel) các buổi TƯƠNG LAI vượt EndDate mới — buổi đã qua luôn được giữ.
    ///     Không hard-delete vì các buổi này có thể đã có Attendance pre-populate sẵn từ Enrollment
    ///     đang Active (khác với RemoveWeeklySlot, chỉ chạy được khi KHÔNG còn Enrollment nào dùng slot).
    ///   - Gia hạn: sinh thêm buổi mới cho từng WeeklySlot hiện có, từ EndDate cũ đến EndDate mới.
    private void RegenerateSessionsForNewEndDate(DateOnly oldEndDate, DateOnly newEndDate) {
        if (newEndDate < oldEndDate) {
            var sessionsToCancel = _classSessions
                .Where(s => s.SessionDate > newEndDate && !s.IsPast() && !s.IsCancelled);

            foreach (var session in sessionsToCancel)
                session.Cancel();

            return;
        }

        if (newEndDate > oldEndDate) {
            foreach (var slot in _weeklySlots) {
                var newSessions = GenerateClassSessions(slot, oldEndDate.AddDays(1), newEndDate);
                _classSessions.AddRange(newSessions);
            }
        }
    }
}