using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.ReplaceLecturer;

public sealed record ReplaceLecturerCommand(
    Guid CourseId,
    Guid NewLecturerId) : IRequest<Result<ReplaceLecturerOutputDto>>;

public sealed class ReplaceLecturerCommandHandler
    : IRequestHandler<ReplaceLecturerCommand, Result<ReplaceLecturerOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;
    private readonly ICourseQueryService _courseQueryService;
    private readonly ICourseUnitOfWork _unitOfWork;

    public ReplaceLecturerCommandHandler(
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService,
        ICourseQueryService courseQueryService,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
        _courseQueryService = courseQueryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReplaceLecturerOutputDto>> Handle(
        ReplaceLecturerCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<ReplaceLecturerOutputDto>(CourseErrors.NotFound);

        // Precondition 1: Lecturer mới phải đang Active.
        var isActiveLecturer = await _lecturerLookupService.IsActiveLecturerAsync(
            request.NewLecturerId, cancellationToken);

        if (!isActiveLecturer)
            return Result.Failure<ReplaceLecturerOutputDto>(CourseErrors.LecturerNotFound);

        // Precondition 2: Date range của course không được overlap với course khác
        // mà Lecturer mới đang phụ trách (exclude chính course này).
        var hasOverlap = await _courseQueryService.HasOverlappingCourseAsync(
            request.NewLecturerId,
            course.StartDate,
            course.EndDate,
            excludeCourseId: course.Id,
            cancellationToken);

        if (hasOverlap)
            return Result.Failure<ReplaceLecturerOutputDto>(CourseErrors.LecturerDateRangeOverlap);

        var newLecturerName = await _lecturerLookupService.GetFullNameAsync(
            request.NewLecturerId, cancellationToken) ?? string.Empty;
 
        var replaceResult = course.ReplaceLecturer(request.NewLecturerId, newLecturerName);
 
        if (replaceResult.IsFailure)
            return Result.Failure<ReplaceLecturerOutputDto>(replaceResult.Error);
 
        _courseRepository.Update(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(CourseMapper.ToReplaceLecturerOutputDto(course, newLecturerName));
    }
}