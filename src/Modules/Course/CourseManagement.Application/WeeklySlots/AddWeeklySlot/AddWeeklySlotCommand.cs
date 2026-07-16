using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.WeeklySlots.AddWeeklySlot;

public sealed record AddWeeklySlotCommand(
    Guid CourseId,
    string DayOfWeek,
    string SessionType) : IRequest<Result<AddWeeklySlotOutputDto>>;

public sealed class AddWeeklySlotCommandHandler
    : IRequestHandler<AddWeeklySlotCommand, Result<AddWeeklySlotOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseQueryService _courseQueryService;
    private readonly ICourseUnitOfWork _unitOfWork;

    public AddWeeklySlotCommandHandler(
        ICourseRepository courseRepository,
        ICourseQueryService courseQueryService,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _courseQueryService = courseQueryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddWeeklySlotOutputDto>> Handle(
        AddWeeklySlotCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<AddWeeklySlotOutputDto>(CourseErrors.NotFound);

        if (!Enum.TryParse<DayOfWeek>(request.DayOfWeek, ignoreCase: true, out var dayOfWeek))
            return Result.Failure<AddWeeklySlotOutputDto>(
                Error.Create("Course.InvalidDayOfWeek", "DayOfWeek is not a valid value."));

        if (!Enum.TryParse<SessionType>(request.SessionType, ignoreCase: true, out var sessionType))
            return Result.Failure<AddWeeklySlotOutputDto>(
                Error.Create("Course.InvalidSessionType", "SessionType must be 'Morning' or 'Afternoon'."));

        // Precondition cross-aggregate: Lecturer không được dạy 2 lớp cùng (thứ, ca) trong các kỳ
        // chồng nhau. Đây là nơi DUY NHẤT check được — CreateCourse chưa biết ca nào, còn tới lúc
        // này thì cả khoảng ngày lẫn ca đều đã xác định. Phải chạy TRƯỚC AddWeeklySlot vì hàm đó
        // sinh luôn hàng loạt ClassSession.
        var hasSlotConflict = await _courseQueryService.HasLecturerSlotConflictAsync(
            course.LecturerId,
            [(dayOfWeek, sessionType)],
            course.StartDate,
            course.EndDate,
            excludeCourseId: course.Id,
            cancellationToken);

        if (hasSlotConflict)
            return Result.Failure<AddWeeklySlotOutputDto>(CourseErrors.LecturerSlotConflict);

        // Domain enforces: Completed immutable, không trùng (DayOfWeek, SessionType).
        // Đồng thời tự sinh toàn bộ ClassSession từ StartDate đến EndDate của Course.
        var addResult = course.AddWeeklySlot(dayOfWeek, sessionType);

        if (addResult.IsFailure)
            return Result.Failure<AddWeeklySlotOutputDto>(addResult.Error);

        var slot = addResult.Value;
        var generatedSessions = course.ClassSessions
            .Where(s => s.WeeklySlotId == slot.Id)
            .ToList();

        _courseRepository.AddWeeklySlot(slot);
        _courseRepository.AddClassSessions(generatedSessions);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddWeeklySlotOutputDto(
            WeeklySlotId: slot.Id,
            CourseId: slot.CourseId,
            DayOfWeek: slot.DayOfWeek.ToString(),
            SessionType: slot.SessionType.ToString(),
            GeneratedSessionCount: generatedSessions.Count));
    }
}