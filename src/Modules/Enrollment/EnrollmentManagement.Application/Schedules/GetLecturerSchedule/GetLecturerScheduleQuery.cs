using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Schedules.GetLecturerSchedule;

// Lecturer lấy toàn bộ lịch dạy để Frontend render dạng timeline.
// Không phân trang — trả về toàn bộ.
public sealed record GetLecturerScheduleQuery(
    Guid LecturerId) : IRequest<Result<GetLecturerScheduleResponse>>;

public sealed record GetLecturerScheduleResponse(
    IReadOnlyList<GetLecturerScheduleOutputDto> Items);

public sealed class GetLecturerScheduleQueryHandler
    : IRequestHandler<GetLecturerScheduleQuery, Result<GetLecturerScheduleResponse>> {
    private readonly ICourseEnrollmentService _courseEnrollmentService;

    public GetLecturerScheduleQueryHandler(ICourseEnrollmentService courseEnrollmentService) {
        _courseEnrollmentService = courseEnrollmentService;
    }

    public async Task<Result<GetLecturerScheduleResponse>> Handle(
        GetLecturerScheduleQuery request,
        CancellationToken cancellationToken) {
        var allCourses = await _courseEnrollmentService.GetCoursesByLecturerAsync(
            request.LecturerId, cancellationToken);

        if (allCourses.Count == 0)
            return Result.Success(new GetLecturerScheduleResponse([]));

        var courseIds = allCourses.Select(c => c.CourseId).ToList();
        var courseMap = allCourses.ToDictionary(c => c.CourseId);

        var sessions = await _courseEnrollmentService.GetClassSessionsByCourseIdsAsync(
            courseIds, cancellationToken);

        var items = sessions
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.SessionType)
            .Select(s => ScheduleMapper.ToGetLecturerScheduleOutputDto(
                s, courseMap[s.CourseId].CourseName))
            .ToList();

        return Result.Success(new GetLecturerScheduleResponse(items));
    }
}