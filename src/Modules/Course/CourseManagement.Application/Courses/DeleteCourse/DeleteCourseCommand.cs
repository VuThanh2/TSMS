using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.DeleteCourse;

public sealed record DeleteCourseCommand(Guid CourseId) : IRequest<Result<DeleteCourseOutputDto>>;

public sealed class DeleteCourseCommandHandler
    : IRequestHandler<DeleteCourseCommand, Result<DeleteCourseOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentCourseService _enrollmentCourseService;
    private readonly ICourseUnitOfWork _unitOfWork;

    public DeleteCourseCommandHandler(
        ICourseRepository courseRepository,
        IEnrollmentCourseService enrollmentCourseService,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _enrollmentCourseService = enrollmentCourseService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeleteCourseOutputDto>> Handle(
        DeleteCourseCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<DeleteCourseOutputDto>(CourseErrors.NotFound);

        // Precondition cross-BC: không cho xóa nếu đã có Student enroll (Enrollment BC sở hữu dữ liệu này).
        var enrolledCount = await _enrollmentCourseService.GetEnrollmentCountAsync(
            request.CourseId, cancellationToken);

        if (enrolledCount > 0)
            return Result.Failure<DeleteCourseOutputDto>(CourseErrors.CourseHasEnrollments);

        // Domain invariant: chỉ xóa được course Upcoming. Raise CourseDeletedEvent (được Outbox
        // bắt từ entity đã tracked ở trạng thái Deleted, trước khi base.SaveChanges chạy).
        var deleteResult = course.Delete();
        if (deleteResult.IsFailure)
            return Result.Failure<DeleteCourseOutputDto>(deleteResult.Error);

        // WeeklySlots + ClassSessions con được DB cascade (OnDelete Cascade).
        _courseRepository.Remove(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new DeleteCourseOutputDto(true));
    }
}
