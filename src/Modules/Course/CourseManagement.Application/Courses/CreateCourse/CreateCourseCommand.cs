using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Entities;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.CreateCourse;

public sealed record CreateCourseCommand(
    Guid LecturerId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaxCapacity) : IRequest<Result<CreateCourseOutputDto>>;

public sealed class CreateCourseCommandHandler
    : IRequestHandler<CreateCourseCommand, Result<CreateCourseOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;
    private readonly ICourseUnitOfWork _unitOfWork;

    public CreateCourseCommandHandler(
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateCourseOutputDto>> Handle(
        CreateCourseCommand request,
        CancellationToken cancellationToken) {
        // Precondition 1: Lecturer phải đang Active.
        var isActiveLecturer = await _lecturerLookupService.IsActiveLecturerAsync(
            request.LecturerId, cancellationToken);

        if (!isActiveLecturer)
            return Result.Failure<CreateCourseOutputDto>(CourseErrors.LecturerNotFound);

        // KHÔNG check trùng lịch dạy ở đây: Course lúc tạo chưa có WeeklySlot nào (Admin thêm sau
        // qua AddWeeklySlot), nên không có "ca" nào để so. Check theo mỗi khoảng ngày là chặn nhầm —
        // 1 Lecturer dạy 2 lớp cùng kỳ khác ca hoàn toàn hợp lệ. Xung đột thật chỉ xác định được
        // khi biết ca cụ thể ⇒ check nằm ở AddWeeklySlotCommand và ReplaceLecturerCommand.

        // Domain factory — validate CourseName, DateRange, MaxCapacity.
        var courseNameResult = CourseName.Create(request.Name);
        if (courseNameResult.IsFailure)
            return Result.Failure<CreateCourseOutputDto>(courseNameResult.Error);

        var dateRangeResult = DateRange.Create(request.StartDate, request.EndDate);
        if (dateRangeResult.IsFailure)
            return Result.Failure<CreateCourseOutputDto>(dateRangeResult.Error);

        var lecturerName = await _lecturerLookupService.GetFullNameAsync(
            request.LecturerId, cancellationToken) ?? string.Empty;
 
        var createResult = Course.Create(
            lecturerId: request.LecturerId,
            courseName: courseNameResult.Value,
            description: request.Description,
            dateRange: dateRangeResult.Value,
            maxCapacity: request.MaxCapacity,
            lecturerName: lecturerName);        
 
        if (createResult.IsFailure)
            return Result.Failure<CreateCourseOutputDto>(createResult.Error);
 
        var course = createResult.Value;
 
        _courseRepository.Add(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
 
        return Result.Success(CourseMapper.ToCreateCourseOutputDto(course, lecturerName));
    }
}