using Reporting.Application.Common.Mappers;
using Reporting.Domain.Errors;
using Reporting.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Reporting.Application.ScoreDistribution.GetScoreDistribution;

public sealed record GetScoreDistributionQuery(
    Guid CourseId) : IRequest<Result<GetScoreDistributionOutputDto>>;

public sealed class GetScoreDistributionQueryHandler
    : IRequestHandler<GetScoreDistributionQuery, Result<GetScoreDistributionOutputDto>> {
    private readonly IReportingRepository _reportingRepository;

    public GetScoreDistributionQueryHandler(IReportingRepository reportingRepository) {
        _reportingRepository = reportingRepository;
    }

    public async Task<Result<GetScoreDistributionOutputDto>> Handle(
        GetScoreDistributionQuery request,
        CancellationToken cancellationToken) {
        var stats = await _reportingRepository.GetCourseStatisticsAsync(
            request.CourseId, cancellationToken);

        if (stats is null)
            return Result.Failure<GetScoreDistributionOutputDto>(ReportingErrors.CourseNotFound);

        // Chưa có Student nào được nhập điểm → trả về items rỗng
        if (stats.GradedStudentCount == 0)
            return Result.Success(new GetScoreDistributionOutputDto(
                stats.CourseId, stats.CourseName, GradedStudentCount: 0, Items: []));

        var distributions = await _reportingRepository.GetScoreDistributionByCourseAsync(
            request.CourseId, cancellationToken);

        var items = distributions.Select(ReportingMapper.ToScoreDistributionItemDto).ToList();

        return Result.Success(new GetScoreDistributionOutputDto(
            stats.CourseId, stats.CourseName, stats.GradedStudentCount, items));
    }
}