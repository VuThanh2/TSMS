using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
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
        // Load kèm WeeklySlots: precondition 2 so từng ca của course này với lịch Lecturer mới.
        // GetByIdAsync thường cho collection rỗng ⇒ check sẽ luôn cho qua, im lặng.
        var course = await _courseRepository.GetByIdWithWeeklySlotsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<ReplaceLecturerOutputDto>(CourseErrors.NotFound);

        // Precondition 1: Lecturer mới phải đang Active.
        var isActiveLecturer = await _lecturerLookupService.IsActiveLecturerAsync(
            request.NewLecturerId, cancellationToken);

        if (!isActiveLecturer)
            return Result.Failure<ReplaceLecturerOutputDto>(CourseErrors.LecturerNotFound);

        // Precondition 2: các ca của course này không được đụng lịch Lecturer mới — xét CẢ ngày
        // lẫn ca. Chỉ trùng khoảng ngày thì không phải xung đột (dạy 2 lớp cùng kỳ khác ca là
        // bình thường), nên không dùng check theo mỗi date range nữa.
        var candidateSlots = course.WeeklySlots
            .Select(s => (s.DayOfWeek, s.SessionType))
            .ToList();

        var hasSlotConflict = await _courseQueryService.HasLecturerSlotConflictAsync(
            request.NewLecturerId,
            candidateSlots,
            course.StartDate,
            course.EndDate,
            excludeCourseId: course.Id,
            cancellationToken);

        if (hasSlotConflict)
            return Result.Failure<ReplaceLecturerOutputDto>(CourseErrors.LecturerSlotConflict);

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