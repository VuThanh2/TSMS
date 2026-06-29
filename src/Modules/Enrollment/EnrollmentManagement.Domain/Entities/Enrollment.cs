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
//   - 2 ClassSessionId phải thuộc đúng Course này (ICourseEnrollmentService).
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

    // sessions: danh sách (ClassSessionId, SessionType) của 2 ca học Student chọn.
    public static Result<Enrollment> Create(
        Guid studentId,
        Guid courseId,
        IReadOnlyList<(Guid ClassSessionId, ValueObjects.SessionType SessionType)> sessions) {
        if (sessions.Count != 2)
            return Result.Failure<Enrollment>(EnrollmentErrors.InvalidSessionCount);

        // Hai session phải khác SessionType (1 Morning + 1 Afternoon).
        if (sessions[0].SessionType == sessions[1].SessionType)
            return Result.Failure<Enrollment>(EnrollmentErrors.DuplicateSessionType);

        var enrollment = new Enrollment {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            Grade = null,
            EnrolledAt = DateTime.UtcNow
        };

        foreach (var (classSessionId, sessionType) in sessions) {
            var enrolledSession = EnrolledSession.Create(enrollment.Id, classSessionId, sessionType);
            enrollment._enrolledSessions.Add(enrolledSession);
        }

        enrollment.RaiseDomainEvent(StudentEnrolledEvent.Create(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.CourseId,
            enrollment.EnrolledAt));

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
    
    // Business rule: Nếu ca cũ đã qua, Attendance record của ca đó được giữ nguyên
    // (buổi điều chỉnh trong tuần đó coi như học thêm).
    public Result AdjustSession(
        Guid oldClassSessionId,
        Guid newClassSessionId,
        SessionType newSessionType) {
        // Guard: không adjust sang chính session đang có.
        if (oldClassSessionId == newClassSessionId)
            return Result.Failure(EnrollmentErrors.SessionAlreadyEnrolled);
 
        var sessionToReplace = _enrolledSessions
            .FirstOrDefault(s => s.ClassSessionId == oldClassSessionId);
 
        if (sessionToReplace is null)
            return Result.Failure(EnrollmentErrors.AdjustSessionNotFound);
 
        // Guard: newClassSessionId không được trùng với session kia đang có.
        var otherSession = _enrolledSessions
            .First(s => s.ClassSessionId != oldClassSessionId);
 
        if (otherSession.ClassSessionId == newClassSessionId)
            return Result.Failure(EnrollmentErrors.SessionAlreadyEnrolled);
 
        // Guard: SessionType sau khi adjust không được trùng nhau.
        if (otherSession.SessionType == newSessionType)
            return Result.Failure(EnrollmentErrors.AdjustSessionTypeDuplicate);
 
        sessionToReplace.Adjust(newClassSessionId, newSessionType);
 
        RaiseDomainEvent(SessionAdjustedEvent.Create(
            Id, StudentId, CourseId, oldClassSessionId, newClassSessionId));
 
        return Result.Success();
    }
}