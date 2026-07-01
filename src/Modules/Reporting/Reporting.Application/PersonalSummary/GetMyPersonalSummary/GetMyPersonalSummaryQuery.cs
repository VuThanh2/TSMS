using Reporting.Application.Common.Interfaces;
using Reporting.Application.Common.Mappers;
using Reporting.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Reporting.Application.PersonalSummary.GetMyPersonalSummary;

// StudentId được lấy từ JWT token tại Presentation Layer.
public sealed record GetMyPersonalSummaryQuery(
    Guid StudentId) : IRequest<Result<GetMyPersonalSummaryOutputDto>>;

public sealed class GetMyPersonalSummaryQueryHandler
    : IRequestHandler<GetMyPersonalSummaryQuery, Result<GetMyPersonalSummaryOutputDto>> {
    private readonly IReportingRepository _reportingRepository;
    private readonly ICourseReportingService _courseReportingService;

    public GetMyPersonalSummaryQueryHandler(
        IReportingRepository reportingRepository,
        ICourseReportingService courseReportingService) {
        _reportingRepository = reportingRepository;
        _courseReportingService = courseReportingService;
    }

    public async Task<Result<GetMyPersonalSummaryOutputDto>> Handle(
        GetMyPersonalSummaryQuery request,
        CancellationToken cancellationToken) {
        var summaries = await _reportingRepository.GetPersonalSummariesByStudentAsync(
            request.StudentId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Mỗi Course có lịch học riêng → cần endedSessionCount theo từng CourseId.
        // Lấy theo batch 1 lần cho toàn bộ Course đã đăng ký, tránh N+1 query.
        var courseIds = summaries.Select(s => s.CourseId).Distinct().ToList();
        var endedSessionCounts = await _courseReportingService.GetEndedSessionCountsAsync(
            courseIds, today, cancellationToken);

        var items = summaries
            .Select(summary => ReportingMapper.ToPersonalSummaryItemDto(
                summary,
                endedSessionCounts.GetValueOrDefault(summary.CourseId)))
            .ToList();

        // overallGpa — Derived Field, tính 1 lần tại đây, không lưu vào Projection table.
        // Chỉ tính trên Course đã có điểm; null nếu chưa có điểm nào.
        var gradedValues = summaries
            .Where(s => s.Grade.HasValue)
            .Select(s => s.Grade!.Value)
            .ToList();

        var overallGpa = gradedValues.Count > 0
            ? Math.Round(gradedValues.Sum() / gradedValues.Count, 2)
            : (decimal?)null;

        return Result.Success(new GetMyPersonalSummaryOutputDto(overallGpa, items));
    }
}