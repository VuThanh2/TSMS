using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Schedules.GetStudentSchedule;

// Student lấy toàn bộ lịch học cá nhân kèm trạng thái điểm danh để render timeline.
// Mỗi WeeklySlot đã chọn (EnrolledSession) áp dụng cho CẢ KỲ — trả về TẤT CẢ ClassSession
// (mọi tuần trong suốt kỳ) thuộc 2 WeeklySlot đã chọn, không chỉ 1 buổi đại diện như trước.
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

        // Lấy TOÀN BỘ ClassSession (mọi tuần) thuộc các WeeklySlot Student đã chọn —
        // 1 EnrolledSession áp dụng cho cả kỳ, không phải chỉ 1 buổi cụ thể.
        var weeklySlotIds = enrollments
            .SelectMany(e => e.EnrolledSessions)
            .Select(s => s.WeeklySlotId)
            .Distinct()
            .ToList();

        var sessions = await _courseEnrollmentService.GetClassSessionsByWeeklySlotIdsAsync(
            weeklySlotIds, cancellationToken);

        var sessionsBySlot = sessions
            .GroupBy(s => s.WeeklySlotId)
            .ToDictionary(g => g.Key, g => g.ToList());

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
                if (!sessionsBySlot.TryGetValue(enrolledSession.WeeklySlotId, out var slotSessions))
                    continue;

                foreach (var session in slotSessions) {
                    var attendanceStatus = attendanceLookup.TryGetValue(
                        session.ClassSessionId, out var status)
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
        }

        var sorted = items
            .OrderBy(i => i.SessionDate)
            .ThenBy(i => i.SessionType)
            .ToList();

        return Result.Success(new GetStudentScheduleResponse(sorted));
    }
}