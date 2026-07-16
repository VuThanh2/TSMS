using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Attendances.GetCourseAttendanceSummary;

// Lecturer xem số liệu điểm danh của TỪNG BUỔI trong 1 Course 
//
// KHÁC GetCourseAttendanceReport bên Reporting BC (dễ nhầm vì tên gần giống):
//   - Reporting gom theo Student cho cả khóa  → "Student A vắng 3/10 buổi"
//   - Query này gom theo ClassSession         → "buổi thứ Tư có 12/15 có mặt"
// Không dùng được Reporting ở đây: projection của nó vứt ClassSessionId đi (chỉ tra theo
// StudentId + CourseId), và nó eventual-consistent — số sẽ cũ ngay trên màn hình Lecturer
// vừa bấm điểm danh. Query này đọc thẳng nguồn sự thật nên luôn tươi.
public sealed record GetCourseAttendanceSummaryQuery(
    Guid CourseId,
    Guid LecturerId) : IRequest<Result<IReadOnlyList<GetCourseAttendanceSummaryOutputDto>>>;

public sealed class GetCourseAttendanceSummaryQueryHandler
    : IRequestHandler<GetCourseAttendanceSummaryQuery, Result<IReadOnlyList<GetCourseAttendanceSummaryOutputDto>>> {
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;

    public GetCourseAttendanceSummaryQueryHandler(
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService) {
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
    }

    public async Task<Result<IReadOnlyList<GetCourseAttendanceSummaryOutputDto>>> Handle(
        GetCourseAttendanceSummaryQuery request,
        CancellationToken cancellationToken) {
        // Precondition: Lecturer phải là người phụ trách Course này (khớp GetSessionAttendances).
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            [request.CourseId], cancellationToken);

        var course = courses.FirstOrDefault(c => c.CourseId == request.CourseId);

        if (course is null || course.LecturerId != request.LecturerId)
            return Result.Failure<IReadOnlyList<GetCourseAttendanceSummaryOutputDto>>(
                EnrollmentErrors.NotCourseOwner);

        var counts = await _attendanceRepository.GetSessionCountsByCourseAsync(
            request.CourseId, cancellationToken);

        var dtos = counts
            .Select(AttendanceMapper.ToGetCourseAttendanceSummaryOutputDto)
            .ToList();

        return Result.Success<IReadOnlyList<GetCourseAttendanceSummaryOutputDto>>(dtos);
    }
}
