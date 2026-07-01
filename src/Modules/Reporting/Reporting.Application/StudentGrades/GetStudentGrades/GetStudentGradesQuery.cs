using Reporting.Application.Common.Mappers;
using Reporting.Domain.Errors;
using Reporting.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Reporting.Application.StudentGrades.GetStudentGrades;

public sealed record GetStudentGradesQuery(
    Guid CourseId) : IRequest<Result<GetStudentGradesOutputDto>>;

public sealed class GetStudentGradesQueryHandler
    : IRequestHandler<GetStudentGradesQuery, Result<GetStudentGradesOutputDto>> {
    private readonly IReportingRepository _reportingRepository;

    public GetStudentGradesQueryHandler(IReportingRepository reportingRepository) {
        _reportingRepository = reportingRepository;
    }

    public async Task<Result<GetStudentGradesOutputDto>> Handle(
        GetStudentGradesQuery request,
        CancellationToken cancellationToken) {
        // Precondition: Course phải tồn tại Projection (đã được tạo qua CourseCreatedEvent).
        var stats = await _reportingRepository.GetCourseStatisticsAsync(
            request.CourseId, cancellationToken);

        if (stats is null)
            return Result.Failure<GetStudentGradesOutputDto>(ReportingErrors.CourseNotFound);

        var gradeReports = await _reportingRepository.GetStudentGradesByCourseAsync(
            request.CourseId, cancellationToken);

        var items = gradeReports.Select(ReportingMapper.ToStudentGradeItemDto).ToList();

        return Result.Success(new GetStudentGradesOutputDto(
            stats.CourseId, stats.CourseName, items));
    }
}