using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Events;
using EnrollmentManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;
using EnrollmentStatus = EnrollmentManagement.Domain.ValueObjects.EnrollmentStatus;
using Grade = EnrollmentManagement.Domain.ValueObjects.Grade;

namespace EnrollmentManagement.Domain.Entities;

// Pre-conditions Application Layer phải đảm bảo trước khi gọi Create():
//   - Student phải Active (IStudentEnrollmentService).
//   - Course phải tồn tại và có Status = Upcoming (ICourseEnrollmentService).
//   - Course chưa đạt MaxCapacity (IEnrollmentRepository.CountActiveEnrollmentsAsync).
//   - Student chưa đăng ký Course này (IEnrollmentRepository.GetByStudentAndCourseAsync).
//   - 2 WeeklySlotId phải thuộc đúng Course này (ICourseEnrollmentService).
public class Enrollment : AggregateRoot {
    private readonly List<EnrolledSession> _enrolledSessions = [];

    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public EnrollmentStatus Status { get; private set; }

    // Null khi chưa được chấm điểm (Status = Active).
    public Grade? Grade { get; private set; }

    public DateTime EnrolledAt { get; private set; }

    public IReadOnlyList<EnrolledSession> EnrolledSessions => _enrolledSessions.AsReadOnly();

    // Required by EF Core.
    private Enrollment() { }

    // slots: danh sách (WeeklySlotId, SessionType) của 2 khung giờ hàng tuần Student chọn cho cả kỳ.
    public static Result<Enrollment> Create(
        Guid studentId,
        Guid courseId,
        IReadOnlyList<(Guid WeeklySlotId, ValueObjects.SessionType SessionType)> slots,
        string studentFullName,        // ← enrich
        string studentEmail,           // ← enrich
        string courseName,             // ← enrich
        string courseStatus,           // ← enrich
        int totalSessionsInCourse) {   // ← enrich

        if (slots.Count != 2)
            return Result.Failure<Enrollment>(EnrollmentErrors.InvalidSessionCount);

        if (slots[0].WeeklySlotId == slots[1].WeeklySlotId)
            return Result.Failure<Enrollment>(EnrollmentErrors.DuplicateSession);

        var enrollment = new Enrollment {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            Grade = null,
            EnrolledAt = DateTime.UtcNow
        };

        foreach (var (weeklySlotId, sessionType) in slots) {
            var enrolledSession = EnrolledSession.Create(enrollment.Id, weeklySlotId, sessionType);
            enrollment._enrolledSessions.Add(enrolledSession);
        }

        enrollment.RaiseDomainEvent(StudentEnrolledEvent.Create(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.CourseId,
            enrollment.EnrolledAt,
            studentFullName,
            studentEmail,
            courseName,
            courseStatus,
            totalSessionsInCourse));

        return Result.Success(enrollment);
    }

    // ── Behaviour methods

    // Pre-condition: Enrollment phải ở trạng thái Active (chưa có điểm).
    public Result AssignGrade(Grade grade) {
        if (Status == EnrollmentStatus.Graded)
            return Result.Failure(EnrollmentErrors.AlreadyGraded);

        Grade = grade;
        Status = EnrollmentStatus.Graded;

        RaiseDomainEvent(GradeAssignedEvent.Create(
            Id, StudentId, CourseId, grade.Value));

        return Result.Success();
    }

    // Pre-condition: Enrollment phải ở trạng thái Graded (đã có điểm).
    public Result UpdateGrade(Grade newGrade) {
        if (Status != EnrollmentStatus.Graded || Grade is null)
            return Result.Failure(EnrollmentErrors.NotYetGraded);

        var previousGrade = Grade.Value;
        Grade = newGrade;

        RaiseDomainEvent(GradeUpdatedEvent.Create(
            Id, StudentId, CourseId, previousGrade, newGrade.Value));

        return Result.Success();
    }

    // Business rule: đổi WeeklySlot chỉ ảnh hưởng các buổi TƯƠNG LAI — Attendance của buổi
    // đã qua thuộc slot cũ được giữ nguyên (coi như học thêm). Đồng bộ Attendance được xử lý
    // ở Application Layer (cross-aggregate, không thuộc trách nhiệm của Enrollment).
    public Result AdjustSession(
        Guid oldWeeklySlotId,
        Guid newWeeklySlotId,
        SessionType newSessionType) {
        // Guard: không adjust sang chính slot đang có.
        if (oldWeeklySlotId == newWeeklySlotId)
            return Result.Failure(EnrollmentErrors.SessionAlreadyEnrolled);

        var sessionToReplace = _enrolledSessions
            .FirstOrDefault(s => s.WeeklySlotId == oldWeeklySlotId);

        if (sessionToReplace is null)
            return Result.Failure(EnrollmentErrors.AdjustSessionNotFound);

        // Guard: newWeeklySlotId không được trùng với slot kia đang có.
        var otherSession = _enrolledSessions
            .First(s => s.WeeklySlotId != oldWeeklySlotId);

        if (otherSession.WeeklySlotId == newWeeklySlotId)
            return Result.Failure(EnrollmentErrors.SessionAlreadyEnrolled);

        // Guard: SessionType sau khi adjust không được trùng nhau.
        if (otherSession.SessionType == newSessionType)
            return Result.Failure(EnrollmentErrors.AdjustSessionTypeDuplicate);

        sessionToReplace.Adjust(newWeeklySlotId, newSessionType);

        RaiseDomainEvent(SessionAdjustedEvent.Create(
            Id, StudentId, CourseId, oldWeeklySlotId, newWeeklySlotId));

        return Result.Success();
    }
}