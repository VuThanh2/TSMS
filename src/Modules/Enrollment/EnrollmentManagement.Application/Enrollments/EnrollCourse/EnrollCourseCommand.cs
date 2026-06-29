using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Enrollments.EnrollCourse;

// StudentId được lấy từ JWT token tại Presentation Layer.
public sealed record EnrollCourseCommand(
    Guid StudentId,
    Guid CourseId,
    IReadOnlyList<Guid> SessionIds) : IRequest<Result<EnrollCourseOutputDto>>;

public sealed class EnrollCourseCommandHandler
    : IRequestHandler<EnrollCourseCommand, Result<EnrollCourseOutputDto>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly IUnitOfWork _unitOfWork;

    public EnrollCourseCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IStudentEnrollmentService studentEnrollmentService,
        IUnitOfWork unitOfWork) {
        _enrollmentRepository = enrollmentRepository;
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _studentEnrollmentService = studentEnrollmentService;
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

        // Precondition 5: Validate 2 SessionIds thuộc đúng Course và parse SessionType.
        var allSessions = await _courseEnrollmentService.GetClassSessionsAsync(
            request.CourseId, cancellationToken);

        var sessionMap = allSessions.ToDictionary(s => s.ClassSessionId);

        var sessionPairs = new List<(Guid ClassSessionId, SessionType SessionType)>();

        foreach (var sessionId in request.SessionIds) {
            if (!sessionMap.TryGetValue(sessionId, out var lookup))
                return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.SessionNotInCourse);

            if (!Enum.TryParse<SessionType>(lookup.SessionType, out var sessionType))
                return Result.Failure<EnrollCourseOutputDto>(EnrollmentErrors.SessionNotInCourse);

            sessionPairs.Add((sessionId, sessionType));
        }

        var enrollmentResult = Enrollment.Create(
            request.StudentId,
            request.CourseId,
            sessionPairs);

        if (enrollmentResult.IsFailure)
            return Result.Failure<EnrollCourseOutputDto>(enrollmentResult.Error);

        var enrollment = enrollmentResult.Value;
        _enrollmentRepository.Add(enrollment);

        var attendances = allSessions
            .Select(s => Attendance.CreateDefault(
                request.StudentId,
                s.ClassSessionId,
                request.CourseId))
            .ToList();

        _attendanceRepository.AddRange(attendances);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(EnrollmentMapper.ToEnrollCourseOutputDto(enrollment, allSessions));
    }
}