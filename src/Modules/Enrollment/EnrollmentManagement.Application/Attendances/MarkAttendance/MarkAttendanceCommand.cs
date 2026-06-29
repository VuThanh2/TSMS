using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Attendances.MarkAttendance;

// LecturerId được lấy từ JWT token tại Presentation Layer.
public sealed record MarkAttendanceCommand(
    Guid AttendanceId,
    Guid LecturerId,
    string AttendanceStatus) : IRequest<Result<MarkAttendanceOutputDto>>;

public sealed class MarkAttendanceCommandHandler
    : IRequestHandler<MarkAttendanceCommand, Result<MarkAttendanceOutputDto>> {
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAttendanceCommandHandler(
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IUnitOfWork unitOfWork) {
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MarkAttendanceOutputDto>> Handle(
        MarkAttendanceCommand request,
        CancellationToken cancellationToken) {
        // Precondition 1: Attendance record phải tồn tại.
        var attendance = await _attendanceRepository.GetByIdAsync(
            request.AttendanceId, cancellationToken);

        if (attendance is null)
            return Result.Failure<MarkAttendanceOutputDto>(EnrollmentErrors.AttendanceNotFound);

        // Precondition 2: Lecturer phải là người phụ trách Course chứa ca học này.
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            [attendance.CourseId], cancellationToken);

        var course = courses.FirstOrDefault(c => c.CourseId == attendance.CourseId);

        if (course is null || course.LecturerId != request.LecturerId)
            return Result.Failure<MarkAttendanceOutputDto>(EnrollmentErrors.NotCourseOwner);

        if (!Enum.TryParse<AttendanceStatus>(request.AttendanceStatus, ignoreCase: true, out var status))
            return Result.Failure<MarkAttendanceOutputDto>(EnrollmentErrors.AttendanceNotFound);

        var markResult = attendance.Mark(status);

        if (markResult.IsFailure)
            return Result.Failure<MarkAttendanceOutputDto>(markResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AttendanceMapper.ToMarkAttendanceOutputDto(attendance));
    }
}