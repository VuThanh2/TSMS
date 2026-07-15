using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.WeeklySlots.RemoveWeeklySlot;

public sealed record RemoveWeeklySlotCommand(
    Guid CourseId,
    Guid WeeklySlotId) : IRequest<Result>;

public sealed class RemoveWeeklySlotCommandHandler
    : IRequestHandler<RemoveWeeklySlotCommand, Result> {
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentCourseService _enrollmentCourseService;
    private readonly ICourseUnitOfWork _unitOfWork;

    public RemoveWeeklySlotCommandHandler(
        ICourseRepository courseRepository,
        IEnrollmentCourseService enrollmentCourseService,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _enrollmentCourseService = enrollmentCourseService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RemoveWeeklySlotCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure(CourseErrors.NotFound);

        // Precondition cross-BC: không cho xóa slot đang có Student enroll (Enrollment BC sở hữu dữ liệu này).
        var isInUse = await _enrollmentCourseService.IsWeeklySlotInUseAsync(
            request.WeeklySlotId, cancellationToken);

        if (isInUse)
            return Result.Failure(CourseErrors.WeeklySlotInUse);

        // Domain enforces: Completed immutable, slot tồn tại, tối thiểu 2 WeeklySlot phải còn lại.
        // ClassSession tương lai bị xóa hẳn (an toàn nhờ check IsWeeklySlotInUseAsync ở trên —
        // không Enrollment nào trỏ tới slot ⇒ không có Attendance tham chiếu) — EF change tracking
        // tự dọn các entity bị gỡ khỏi collection đã tracked từ GetByIdWithSessionsAsync
        // (cascade delete), không cần gọi thêm repository method nào.
        var removeResult = course.RemoveWeeklySlot(request.WeeklySlotId);

        if (removeResult.IsFailure)
            return removeResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}