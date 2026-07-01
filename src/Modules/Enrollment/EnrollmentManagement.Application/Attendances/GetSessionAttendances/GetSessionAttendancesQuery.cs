using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Attendances.GetSessionAttendances;

// Lecturer xem danh sách điểm danh của tất cả Student trong một ca học.
// LecturerId được lấy từ JWT token tại Presentation Layer — verify course ownership.
public sealed record GetSessionAttendancesQuery(
    Guid CourseId,
    Guid ClassSessionId,
    Guid LecturerId) : IRequest<Result<IReadOnlyList<GetSessionAttendancesOutputDto>>>;

public sealed class GetSessionAttendancesQueryHandler
    : IRequestHandler<GetSessionAttendancesQuery, Result<IReadOnlyList<GetSessionAttendancesOutputDto>>> {
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IStudentEnrollmentService _studentEnrollmentService;

    public GetSessionAttendancesQueryHandler(
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IStudentEnrollmentService studentEnrollmentService) {
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _studentEnrollmentService = studentEnrollmentService;
    }

    public async Task<Result<IReadOnlyList<GetSessionAttendancesOutputDto>>> Handle(
        GetSessionAttendancesQuery request,
        CancellationToken cancellationToken) {
        // Precondition: Lecturer phải là người phụ trách Course này.
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            [request.CourseId], cancellationToken);

        var course = courses.FirstOrDefault(c => c.CourseId == request.CourseId);

        if (course is null || course.LecturerId != request.LecturerId)
            return Result.Failure<IReadOnlyList<GetSessionAttendancesOutputDto>>(
                EnrollmentErrors.NotCourseOwner);

        var attendances = await _attendanceRepository.GetBySessionIdAsync(
            request.ClassSessionId, cancellationToken);

        var dtos = new List<GetSessionAttendancesOutputDto>(attendances.Count);

        foreach (var attendance in attendances) {
            var fullName = await _studentEnrollmentService.GetFullNameAsync(
                attendance.StudentId, cancellationToken);

            dtos.Add(AttendanceMapper.ToGetSessionAttendancesOutputDto(attendance, fullName));
        }

        return Result.Success<IReadOnlyList<GetSessionAttendancesOutputDto>>(dtos);
    }
}