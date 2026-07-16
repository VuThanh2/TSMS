using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.GetAvailableCourses;

// "Available" = Upcoming + Admin đã mở đăng ký + Student chưa đăng ký.
// Course chưa mở là course Admin đang dựng dở → không được lộ cho Student.
public sealed record GetAvailableCoursesQuery(
    Guid StudentId,
    int Page,
    int PageSize) : IRequest<Result<PagedList<GetAvailableCoursesOutputDto>>>;

public sealed class GetAvailableCoursesQueryHandler
    : IRequestHandler<GetAvailableCoursesQuery, Result<PagedList<GetAvailableCoursesOutputDto>>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;
    private readonly IEnrollmentCourseService _enrollmentCourseService;

    public GetAvailableCoursesQueryHandler(
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService,
        IEnrollmentCourseService enrollmentCourseService) {
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
        _enrollmentCourseService = enrollmentCourseService;
    }

    public async Task<Result<PagedList<GetAvailableCoursesOutputDto>>> Handle(
        GetAvailableCoursesQuery request,
        CancellationToken cancellationToken) {
        // Lấy toàn bộ Upcoming courses — không filter lecturerId, Student xem được tất cả.
        var (allUpcoming, totalUpcomingCount) = await _courseRepository.GetPagedAsync(
            keyword: null,
            status: CourseStatus.Upcoming,
            lecturerId: null,
            page: 1,
            pageSize: int.MaxValue,
            cancellationToken: cancellationToken);

        // Lọc ra các course Student đã enroll để exclude.
        var enrolledCourseIds = await _enrollmentCourseService.GetEnrolledCourseIdsAsync(
            request.StudentId, cancellationToken);

        var available = allUpcoming
            .Where(c => c.IsOpenForEnrollment)
            .Where(c => !enrolledCourseIds.Contains(c.Id))
            .ToList();

        var totalCount = available.Count;
        var pageItems = available
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = new List<GetAvailableCoursesOutputDto>(pageItems.Count);
        foreach (var course in pageItems) {
            var lecturerName = await _lecturerLookupService.GetFullNameAsync(
                course.LecturerId, cancellationToken);
            var enrolledCount = await _enrollmentCourseService.GetEnrollmentCountAsync(
                course.Id, cancellationToken);
            dtos.Add(CourseMapper.ToGetAvailableCoursesOutputDto(course, lecturerName, enrolledCount));
        }

        return Result.Success(PagedList<GetAvailableCoursesOutputDto>.Create(
            dtos, request.Page, request.PageSize, totalCount));
    }
}