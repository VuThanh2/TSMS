using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Schedules.GetStudentSchedule;

// Student lấy toàn bộ lịch học cá nhân kèm trạng thái điểm danh để render timeline.
// Chỉ trả về 2 ca học đã chọn trong mỗi Course đã đăng ký (EnrolledSessions).
// Không phân trang — trả về toàn bộ.
public sealed record GetStudentScheduleQuery(
    Guid StudentId) : IRequest<Result<GetStudentScheduleResponse>>;

public sealed record GetStudentScheduleResponse(
    IReadOnlyList<GetStudentScheduleOutputDto> Items);

public sealed class GetStudentScheduleQueryHandler
    : IRequestHandler<GetStudentScheduleQuery, Result<GetStudentScheduleResponse>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;

    public GetStudentScheduleQueryHandler(
        IEnrollmentRepository enrollmentRepository,
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService) {
        _enrollmentRepository = enrollmentRepository;
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
    }

    public async Task<Result<GetStudentScheduleResponse>> Handle(
        GetStudentScheduleQuery request,
        CancellationToken cancellationToken) {
        var enrollments = await _enrollmentRepository.GetByStudentIdAsync(
            request.StudentId, cancellationToken);

        if (enrollments.Count == 0)
            return Result.Success(new GetStudentScheduleResponse([]));

        var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            courseIds, cancellationToken);
        var courseMap = courses.ToDictionary(c => c.CourseId);

        var allSessions = await _courseEnrollmentService.GetClassSessionsByCourseIdsAsync(
            courseIds, cancellationToken);
        var sessionMap = allSessions.ToDictionary(s => s.ClassSessionId);

        // Attendance lookup: classSessionId → status string
        var attendanceLookup = new Dictionary<Guid, string>();
        foreach (var enrollment in enrollments) {
            var attendances = await _attendanceRepository.GetByStudentAndCourseAsync(
                request.StudentId, enrollment.CourseId, cancellationToken);
            foreach (var att in attendances)
                attendanceLookup[att.ClassSessionId] = att.Status.ToString();
        }

        var items = new List<GetStudentScheduleOutputDto>();

        foreach (var enrollment in enrollments) {
            if (!courseMap.TryGetValue(enrollment.CourseId, out var course))
                continue;

            foreach (var enrolledSession in enrollment.EnrolledSessions) {
                if (!sessionMap.TryGetValue(enrolledSession.ClassSessionId, out var session))
                    continue;

                var attendanceStatus = attendanceLookup.TryGetValue(
                    enrolledSession.ClassSessionId, out var status)
                    ? status
                    : "Absent";

                items.Add(ScheduleMapper.ToGetStudentScheduleOutputDto(
                    courseId: enrollment.CourseId,
                    courseName: course.CourseName,
                    enrollmentId: enrollment.Id,
                    session: session,
                    sessionType: enrolledSession.SessionType.ToString(),
                    attendanceStatus: attendanceStatus));
            }
        }

        var sorted = items
            .OrderBy(i => i.SessionDate)
            .ThenBy(i => i.SessionType)
            .ToList();

        return Result.Success(new GetStudentScheduleResponse(sorted));
    }
}