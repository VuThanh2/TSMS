using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.GetCourseById;

// Presentation Layer chịu trách nhiệm kiểm tra ownership nếu role là Lecturer.
public sealed record GetCourseByIdQuery(Guid CourseId)
    : IRequest<Result<GetCourseByIdOutputDto>>;

public sealed class GetCourseByIdQueryHandler
    : IRequestHandler<GetCourseByIdQuery, Result<GetCourseByIdOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;
    private readonly IEnrollmentCourseService _enrollmentCourseService;

    public GetCourseByIdQueryHandler(
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService,
        IEnrollmentCourseService enrollmentCourseService) {
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
        _enrollmentCourseService = enrollmentCourseService;
    }

    public async Task<Result<GetCourseByIdOutputDto>> Handle(
        GetCourseByIdQuery request,
        CancellationToken cancellationToken) {
        // Load with sessions — GetCourseById luôn trả về ClassSessions.
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<GetCourseByIdOutputDto>(CourseErrors.NotFound);

        var lecturerName = await _lecturerLookupService.GetFullNameAsync(
            course.LecturerId, cancellationToken);
        var enrolledCount = await _enrollmentCourseService.GetEnrollmentCountAsync(
            course.Id, cancellationToken);

        return Result.Success(CourseMapper.ToGetCourseByIdOutputDto(course, lecturerName, enrolledCount));
    }
}