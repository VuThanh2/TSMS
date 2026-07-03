using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Enrollments.AdjustSession;

// StudentId được lấy từ JWT token tại Presentation Layer.
public sealed record AdjustSessionCommand(
    Guid EnrollmentId,
    Guid StudentId,
    Guid OldWeeklySlotId,
    Guid NewWeeklySlotId) : IRequest<Result<AdjustSessionOutputDto>>;

public sealed class AdjustSessionCommandHandler
    : IRequestHandler<AdjustSessionCommand, Result<AdjustSessionOutputDto>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IScheduleConflictChecker _scheduleConflictChecker;
    private readonly IEnrollmentUnitOfWork _unitOfWork;

    public AdjustSessionCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IScheduleConflictChecker scheduleConflictChecker,
        IEnrollmentUnitOfWork unitOfWork) {
        _enrollmentRepository = enrollmentRepository;
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _scheduleConflictChecker = scheduleConflictChecker;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdjustSessionOutputDto>> Handle(
        AdjustSessionCommand request,
        CancellationToken cancellationToken) {
        // Precondition 1: Enrollment phải tồn tại và thuộc Student này.
        var enrollment = await _enrollmentRepository.GetByIdAsync(
            request.EnrollmentId, cancellationToken);

        if (enrollment is null || enrollment.StudentId != request.StudentId)
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.NotFound);

        // Precondition 2: Course không được ở trạng thái Completed.
        var courseStatus = await _courseEnrollmentService.GetStatusAsync(
            enrollment.CourseId, cancellationToken);

        if (courseStatus == "Completed")
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.CourseAlreadyCompleted);

        // Precondition 3: NewWeeklySlotId phải thuộc Course này, lấy SessionType.
        var courseSlots = await _courseEnrollmentService.GetWeeklySlotsAsync(
            enrollment.CourseId, cancellationToken);

        var newSlotLookup = courseSlots.FirstOrDefault(s => s.WeeklySlotId == request.NewWeeklySlotId);

        if (newSlotLookup is null)
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.SessionNotInCourse);

        if (!Enum.TryParse<SessionType>(newSlotLookup.SessionType, out var newSessionType))
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.SessionNotInCourse);

        // Precondition 4: Slot mới không được trùng (DayOfWeek, SessionType) với Course khác
        // Student đã đăng ký.
        var candidateSlots = new List<(DayOfWeek DayOfWeek, string SessionType)> {
            (Enum.Parse<DayOfWeek>(newSlotLookup.DayOfWeek), newSlotLookup.SessionType)
        };

        var hasConflict = await _scheduleConflictChecker.HasConflictAsync(
            request.StudentId, enrollment.CourseId, candidateSlots, cancellationToken);

        if (hasConflict)
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.ScheduleConflict);

        // Domain behaviour — guards bên trong: oldSlot tồn tại, không trùng slot kia đang có,
        // không trùng SessionType với slot kia.
        var adjustResult = enrollment.AdjustSession(
            request.OldWeeklySlotId,
            request.NewWeeklySlotId,
            newSessionType);

        if (adjustResult.IsFailure)
            return Result.Failure<AdjustSessionOutputDto>(adjustResult.Error);

        // Đồng bộ Attendance — chỉ tác động buổi TƯƠNG LAI, buổi đã qua giữ nguyên
        // (business rule: coi như buổi học thêm, không xóa lịch sử điểm danh).
        await SyncFutureAttendanceAsync(
            request.StudentId,
            enrollment.CourseId,
            request.OldWeeklySlotId,
            request.NewWeeklySlotId,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(EnrollmentMapper.ToAdjustSessionOutputDto(enrollment, courseSlots));
    }

    // ── Private helpers

    // Xóa Attendance các buổi tương lai thuộc slot cũ, tạo Attendance mặc định (Absent)
    // cho các buổi tương lai thuộc slot mới. Cross-aggregate orchestration — không thuộc
    // trách nhiệm của Enrollment aggregate, nên xử lý ở đây thay vì trong Domain.
    private async Task SyncFutureAttendanceAsync(
        Guid studentId,
        Guid courseId,
        Guid oldWeeklySlotId,
        Guid newWeeklySlotId,
        CancellationToken cancellationToken) {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var oldSlotSessions = await _courseEnrollmentService.GetClassSessionsByWeeklySlotIdsAsync(
            [oldWeeklySlotId], cancellationToken);

        var oldFutureSessionIds = oldSlotSessions
            .Where(s => s.SessionDate >= today)
            .Select(s => s.ClassSessionId)
            .ToList();

        if (oldFutureSessionIds.Count > 0) {
            var attendancesToRemove = await _attendanceRepository.GetByStudentAndSessionIdsAsync(
                studentId, oldFutureSessionIds, cancellationToken);

            _attendanceRepository.RemoveRange(attendancesToRemove);
        }

        var newSlotSessions = await _courseEnrollmentService.GetClassSessionsByWeeklySlotIdsAsync(
            [newWeeklySlotId], cancellationToken);

        var newFutureSessions = newSlotSessions
            .Where(s => s.SessionDate >= today)
            .ToList();

        if (newFutureSessions.Count > 0) {
            var newAttendances = newFutureSessions
                .Select(s => Attendance.CreateDefault(studentId, s.ClassSessionId, courseId))
                .ToList();

            _attendanceRepository.AddRange(newAttendances);
        }
    }
}