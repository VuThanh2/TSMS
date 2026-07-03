using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Enrollments.EnrollCourse;

// StudentId được lấy từ JWT token tại Presentation Layer.
public sealed record EnrollCourseCommand(
    Guid StudentId,
    Guid CourseId,
    IReadOnlyList<Guid> WeeklySlotIds) : IRequest<Result<EnrollCourseOutputDto>>;

public sealed class EnrollCourseCommandHandler
    : IRequestHandler<EnrollCourseCommand, Result<EnrollCourseOutputDto>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly IScheduleConflictChecker _scheduleConflictChecker;
    private readonly IEnrollmentUnitOfWork _unitOfWork;

    public EnrollCourseCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IStudentEnrollmentService studentEnrollmentService,
        IScheduleConflictChecker scheduleConflictChecker,
        IEnrollmentUnitOfWork unitOfWork) {
        _enrollmentRepository = enrollmentRepository;
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _studentEnrollmentService = studentEnrollmentService;
        _scheduleConflictChecker = scheduleConflictChecker;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EnrollCourseOutputDto>> Handle(
        EnrollCourseCommand request,
        CancellationToken cancellationToken) {
        // Precondition 1: Student phải đang Active.
        var isActiveStudent = await _studentEnrollmentService.IsActiveStudentAsync(
            request.StudentId, cancellationToken);

        if (!isActiveStudent)
            return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.StudentNotActive);

        // Precondition 2: Course phải Upcoming.
        var isUpcoming = await _courseEnrollmentService.IsUpcomingAsync(
            request.CourseId, cancellationToken);

        if (!isUpcoming)
            return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.CourseNotEnrollable);

        // Precondition 3: Student chưa đăng ký Course này.
        var existing = await _enrollmentRepository.GetByStudentAndCourseAsync(
            request.StudentId, request.CourseId, cancellationToken);

        if (existing is not null)
            return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.AlreadyEnrolled);

        // Precondition 4: Course chưa đạt MaxCapacity.
        var maxCapacity = await _courseEnrollmentService.GetMaxCapacityAsync(
            request.CourseId, cancellationToken);

        var currentCount = await _enrollmentRepository.CountActiveEnrollmentsAsync(
            request.CourseId, cancellationToken);

        if (maxCapacity.HasValue && currentCount >= maxCapacity.Value)
            return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.CourseIsFull);

        // Precondition 5: Validate 2 WeeklySlotIds thuộc đúng Course và parse SessionType.
        var courseSlots = await _courseEnrollmentService.GetWeeklySlotsAsync(
            request.CourseId, cancellationToken);

        var slotMap = courseSlots.ToDictionary(s => s.WeeklySlotId);

        var slotPairs = new List<(Guid WeeklySlotId, SessionType SessionType)>();

        foreach (var weeklySlotId in request.WeeklySlotIds) {
            if (!slotMap.TryGetValue(weeklySlotId, out var lookup))
                return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.SessionNotInCourse);

            if (!Enum.TryParse<SessionType>(lookup.SessionType, out var sessionType))
                return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.SessionNotInCourse);

            slotPairs.Add((weeklySlotId, sessionType));
        }

        // Precondition 6: 2 WeeklySlot không được trùng (DayOfWeek, SessionType) với Course khác
        // Student đã đăng ký — trùng lịch lặp lại hàng tuần, không chỉ 1 ngày cụ thể.
        var candidateSlots = request.WeeklySlotIds
            .Select(id => (
                DayOfWeek: Enum.Parse<DayOfWeek>(slotMap[id].DayOfWeek),
                SessionType: slotMap[id].SessionType))
            .ToList();

        var hasConflict = await _scheduleConflictChecker.HasConflictAsync(
            request.StudentId, request.CourseId, candidateSlots, cancellationToken);

        if (hasConflict)
            return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.ScheduleConflict);

        var studentFullName = await _studentEnrollmentService.GetFullNameAsync(
            request.StudentId, cancellationToken) ?? string.Empty;

        var studentEmail = await _studentEnrollmentService.GetEmailAsync(
            request.StudentId, cancellationToken) ?? string.Empty;

        var courseStatus = await _courseEnrollmentService.GetStatusAsync(
            request.CourseId, cancellationToken) ?? string.Empty;

        var courseLookups = await _courseEnrollmentService.GetCoursesByIdsAsync(
            [request.CourseId], cancellationToken);
        var courseName = courseLookups.FirstOrDefault()?.CourseName ?? string.Empty;

        // TotalSessionsInCourse (cho StudentEnrolledEvent/Reporting) = TOÀN BỘ ClassSession của Course,
        // KHÔNG phải chỉ các buổi Student sẽ tham dự — 2 khái niệm khác nhau, tách riêng nguồn dữ liệu.
        var allCourseSessions = await _courseEnrollmentService.GetClassSessionsAsync(
            request.CourseId, cancellationToken);

        var enrollmentResult = Enrollment.Create(
            request.StudentId,
            request.CourseId,
            slotPairs,
            studentFullName,
            studentEmail,
            courseName,
            courseStatus,
            totalSessionsInCourse: allCourseSessions.Count);

        if (enrollmentResult.IsFailure)
            return Result.Failure<EnrollCourseOutputDto>(enrollmentResult.Error);

        var enrollment = enrollmentResult.Value;
        _enrollmentRepository.Add(enrollment);

        // Attendance CHỈ sinh cho ClassSession thuộc đúng 2 WeeklySlot Student chọn —
        // KHÔNG sinh cho toàn bộ ClassSession của Course
        var enrolledClassSessions = await _courseEnrollmentService.GetClassSessionsByWeeklySlotIdsAsync(
            request.WeeklySlotIds, cancellationToken);

        var attendances = enrolledClassSessions
            .Select(s => Attendance.CreateDefault(
                request.StudentId,
                s.ClassSessionId,
                request.CourseId))
            .ToList();

        _attendanceRepository.AddRange(attendances);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(EnrollmentMapper.ToEnrollCourseOutputDto(enrollment, courseSlots));
    }
}