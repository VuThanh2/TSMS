using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.UpdateCourse;

public sealed record UpdateCourseCommand(
    Guid CourseId,
    string Name,
    string? Description,
    DateOnly EndDate,
    int MaxCapacity) : IRequest<Result<UpdateCourseOutputDto>>;

public sealed class UpdateCourseCommandHandler
    : IRequestHandler<UpdateCourseCommand, Result<UpdateCourseOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;
    private readonly IEnrollmentCourseService _enrollmentCourseService;
    private readonly ICourseUnitOfWork _unitOfWork;

    public UpdateCourseCommandHandler(
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService,
        IEnrollmentCourseService enrollmentCourseService,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
        _enrollmentCourseService = enrollmentCourseService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UpdateCourseOutputDto>> Handle(
        UpdateCourseCommand request,
        CancellationToken cancellationToken) {
        // Load with sessions — Domain cần kiểm tra EndDate >= latest ClassSession.
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<UpdateCourseOutputDto>(CourseErrors.NotFound);

        // Precondition: maxCapacity mới không được nhỏ hơn số sinh viên đã đăng ký.
        var enrolledCount = await _enrollmentCourseService.GetEnrollmentCountAsync(
            request.CourseId, cancellationToken);

        if (request.MaxCapacity < enrolledCount)
            return Result.Failure<UpdateCourseOutputDto>(CourseErrors.MaxCapacityBelowEnrolledCount);

        var courseNameResult = CourseName.Create(request.Name);
        if (courseNameResult.IsFailure)
            return Result.Failure<UpdateCourseOutputDto>(courseNameResult.Error);

        // Domain enforces: Completed course immutable, EndDate >= latest session, EndDate > today.
        var updateResult = course.UpdateInfo(
            courseNameResult.Value,
            request.Description,
            request.EndDate,
            request.MaxCapacity);

        if (updateResult.IsFailure)
            return Result.Failure<UpdateCourseOutputDto>(updateResult.Error);

        _courseRepository.Update(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lecturerName = await _lecturerLookupService.GetFullNameAsync(
            course.LecturerId, cancellationToken);

        return Result.Success(CourseMapper.ToUpdateCourseOutputDto(course, lecturerName));
    }
}