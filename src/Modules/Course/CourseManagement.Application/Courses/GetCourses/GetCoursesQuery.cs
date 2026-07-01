using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.GetCourses;

// Role scoping được thực hiện ở tầng Presentation (truyền lecturerId nếu Lecturer).
// Admin/Student truyền lecturerId = null để xem toàn bộ.
public sealed record GetCoursesQuery(
    string? Keyword,
    string? Status,
    Guid? LecturerId,
    int Page,
    int PageSize) : IRequest<Result<PagedList<GetCoursesOutputDto>>>;

public sealed class GetCoursesQueryHandler
    : IRequestHandler<GetCoursesQuery, Result<PagedList<GetCoursesOutputDto>>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;

    public GetCoursesQueryHandler(
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService) {
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
    }

    public async Task<Result<PagedList<GetCoursesOutputDto>>> Handle(
        GetCoursesQuery request,
        CancellationToken cancellationToken) {
        CourseStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<CourseStatus>(request.Status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var (items, totalCount) = await _courseRepository.GetPagedAsync(
            keyword: request.Keyword,
            status: statusFilter,
            lecturerId: request.LecturerId,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        // Enrich với LecturerName — số item tối đa là pageSize, nên N calls là chấp nhận được.
        var dtos = new List<GetCoursesOutputDto>(items.Count);
        foreach (var course in items) {
            var lecturerName = await _lecturerLookupService.GetFullNameAsync(
                course.LecturerId, cancellationToken);
            dtos.Add(CourseMapper.ToGetCoursesOutputDto(course, lecturerName));
        }

        return Result.Success(PagedList<GetCoursesOutputDto>.Create(
            dtos, request.Page, request.PageSize, totalCount));
    }
}