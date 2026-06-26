using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.ClassSessions.GetClassSessions;

// Dùng chung cho cả Admin và Lecturer (Presentation tự enforce ownership).
public sealed record GetClassSessionsQuery(Guid CourseId)
    : IRequest<Result<IReadOnlyList<GetClassSessionsOutputDto>>>;

public sealed class GetClassSessionsQueryHandler
    : IRequestHandler<GetClassSessionsQuery, Result<IReadOnlyList<GetClassSessionsOutputDto>>> {
    private readonly ICourseRepository _courseRepository;

    public GetClassSessionsQueryHandler(ICourseRepository courseRepository) {
        _courseRepository = courseRepository;
    }

    public async Task<Result<IReadOnlyList<GetClassSessionsOutputDto>>> Handle(
        GetClassSessionsQuery request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<IReadOnlyList<GetClassSessionsOutputDto>>(CourseErrors.NotFound);

        var dtos = course.ClassSessions
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.SessionType)
            .Select(ClassSessionMapper.ToGetClassSessionsOutputDto)
            .ToList();

        return Result.Success<IReadOnlyList<GetClassSessionsOutputDto>>(dtos);
    }
}