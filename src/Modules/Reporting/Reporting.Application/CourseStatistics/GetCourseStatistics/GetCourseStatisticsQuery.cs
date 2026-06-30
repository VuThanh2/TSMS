using Reporting.Application.Common.Mappers;
using Reporting.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Reporting.Application.CourseStatistics.GetCourseStatistics;

public sealed record GetCourseStatisticsQuery : IRequest<Result<GetCourseStatisticsOutputDto>>;

public sealed class GetCourseStatisticsQueryHandler
    : IRequestHandler<GetCourseStatisticsQuery, Result<GetCourseStatisticsOutputDto>> {
    private readonly IReportingRepository _reportingRepository;

    public GetCourseStatisticsQueryHandler(IReportingRepository reportingRepository) {
        _reportingRepository = reportingRepository;
    }

    public async Task<Result<GetCourseStatisticsOutputDto>> Handle(
        GetCourseStatisticsQuery request,
        CancellationToken cancellationToken) {
        var views = await _reportingRepository.GetAllCourseStatisticsAsync(cancellationToken);

        var items = views.Select(ReportingMapper.ToCourseStatisticsItemDto).ToList();

        return Result.Success(new GetCourseStatisticsOutputDto(items.Count, items));
    }
}