using Reporting.Application.Common.Interfaces;
using Reporting.Application.Common.Mappers;
using Reporting.Domain.Errors;
using Reporting.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Reporting.Application.Attendance.GetCourseAttendanceReport;

// Admin xem tất cả Course; Lecturer chỉ xem Course mình phụ trách (UC-31).
// LecturerId = null khi caller là Admin — bỏ qua ownership check.
public sealed record GetCourseAttendanceReportQuery(
    Guid CourseId,
    Guid? LecturerId) : IRequest<Result<GetCourseAttendanceReportDto>>;

public sealed class GetCourseAttendanceReportQueryHandler
    : IRequestHandler<GetCourseAttendanceReportQuery, Result<GetCourseAttendanceReportDto>> {
    private readonly IReportingRepository _reportingRepository;
    private readonly ICourseReportingService _courseReportingService;

    public GetCourseAttendanceReportQueryHandler(
        IReportingRepository reportingRepository,
        ICourseReportingService courseReportingService) {
        _reportingRepository = reportingRepository;
        _courseReportingService = courseReportingService;
    }

    public async Task<Result<GetCourseAttendanceReportDto>> Handle(
        GetCourseAttendanceReportQuery request,
        CancellationToken cancellationToken) {
        var stats = await _reportingRepository.GetCourseStatisticsAsync(
            request.CourseId, cancellationToken);

        if (stats is null)
            return Result.Failure<GetCourseAttendanceReportDto>(ReportingErrors.CourseNotFound);

        // Precondition: nếu caller là Lecturer, chỉ được xem Course mình phụ trách.
        if (request.LecturerId.HasValue && stats.LecturerId != request.LecturerId.Value)
            return Result.Failure<GetCourseAttendanceReportDto>(ReportingErrors.NotCourseOwner);

        // attendanceRate chỉ tính trên các ca có sessionDate <= today (Rule – Attendance Stats
        // Exclude Future Sessions) — số ca này do CourseManagement Context cung cấp.
        var endedSessionCount = await _courseReportingService.GetEndedSessionCountAsync(
            request.CourseId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

        var reports = await _reportingRepository.GetAttendanceReportByCourseAsync(
            request.CourseId, cancellationToken);

        var items = reports
            .Select(r => ReportingMapper.ToCourseAttendanceItemDto(r, endedSessionCount))
            .ToList();

        return Result.Success(new GetCourseAttendanceReportDto(
            stats.CourseId, stats.CourseName, items));
    }
}