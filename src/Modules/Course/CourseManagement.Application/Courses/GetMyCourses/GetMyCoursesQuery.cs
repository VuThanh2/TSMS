using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.GetMyCourses;

// ── UC-22: Student xem danh sách khóa học đã đăng ký kèm điểm số.
public sealed record GetMyCoursesQuery(Guid StudentId)
    : IRequest<Result<IReadOnlyList<GetMyCoursesOutputDto>>>;

public sealed class GetMyCoursesQueryHandler
    : IRequestHandler<GetMyCoursesQuery, Result<IReadOnlyList<GetMyCoursesOutputDto>>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;
    private readonly IEnrollmentLookupService _enrollmentLookupService;

    public GetMyCoursesQueryHandler(
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService,
        IEnrollmentLookupService enrollmentLookupService) {
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
        _enrollmentLookupService = enrollmentLookupService;
    }

    public async Task<Result<IReadOnlyList<GetMyCoursesOutputDto>>> Handle(
        GetMyCoursesQuery request,
        CancellationToken cancellationToken) {
        // Lấy courseIds và grades trong một lần call để tránh multiple round-trips.
        var gradesByCourse = await _enrollmentLookupService.GetGradesByCourseAsync(
            request.StudentId, cancellationToken);

        if (gradesByCourse.Count == 0)
            return Result.Success<IReadOnlyList<GetMyCoursesOutputDto>>([]);

        // Batch fetch — tránh N+1 so với GetByIdAsync từng cái một.
        var courses = await _courseRepository.GetByIdsAsync(
            gradesByCourse.Keys, cancellationToken);

        var dtos = new List<GetMyCoursesOutputDto>(courses.Count);
        foreach (var course in courses) {
            var lecturerName = await _lecturerLookupService.GetFullNameAsync(
                course.LecturerId, cancellationToken);

            // Keys đã được verify bởi GetByIdsAsync — indexer an toàn.
            dtos.Add(new GetMyCoursesOutputDto(
                CourseId: course.Id,
                Name: course.Name,
                Status: course.Status.ToString(),
                StartDate: course.StartDate,
                EndDate: course.EndDate,
                LecturerId: course.LecturerId,
                LecturerName: lecturerName,
                Grade: gradesByCourse[course.Id]));
        }

        return Result.Success<IReadOnlyList<GetMyCoursesOutputDto>>(dtos);
    }
}