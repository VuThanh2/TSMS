using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Attendances.GetSessionAttendances;

// Lecturer xem danh sách điểm danh của tất cả Student trong một ca học.
// LecturerId được lấy từ JWT token tại Presentation Layer — verify course ownership.
//
// KHÔNG nhận CourseId từ caller: Course sở hữu ca học phải được suy ra từ chính ClassSessionId
// (xem handler). Nếu nhận CourseId rời rồi chỉ check "Lecturer có sở hữu CourseId đó không",
// caller có thể ghép CourseId mình sở hữu với ClassSessionId của Course khác và đọc trộm điểm
// danh của Course đó — cùng lý do MarkAttendanceCommand suy CourseId từ chính bản ghi Attendance.
public sealed record GetSessionAttendancesQuery(
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
        // Ca học phải tồn tại — đồng thời đây là nguồn DUY NHẤT xác định Course sở hữu nó.
        var classSession = await _courseEnrollmentService.GetClassSessionAsync(
            request.ClassSessionId, cancellationToken);

        if (classSession is null)
            return Result.Failure<IReadOnlyList<GetSessionAttendancesOutputDto>>(
                EnrollmentErrors.ClassSessionNotFound);

        // Precondition: Lecturer phải là người phụ trách đúng Course chứa ca học này.
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            [classSession.CourseId], cancellationToken);

        var course = courses.FirstOrDefault(c => c.CourseId == classSession.CourseId);

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