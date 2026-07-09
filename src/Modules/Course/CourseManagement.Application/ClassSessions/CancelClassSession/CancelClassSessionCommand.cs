using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.ClassSessions.CancelClassSession;

public sealed record CancelClassSessionCommand(
    Guid CourseId,
    Guid ClassSessionId) : IRequest<Result>;

public sealed class CancelClassSessionCommandHandler
    : IRequestHandler<CancelClassSessionCommand, Result> {
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseUnitOfWork _unitOfWork;

    public CancelClassSessionCommandHandler(
        ICourseRepository courseRepository,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        CancelClassSessionCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure(CourseErrors.NotFound);

        // Domain enforces: session exists, not past, chưa bị hủy trước đó.
        // Soft-cancel — không xóa vật lý, EF change tracking tự lưu thay đổi IsCancelled
        // trên entity đã tracked, không cần gọi thêm repository method nào.
        var cancelResult = course.CancelClassSession(request.ClassSessionId);

        if (cancelResult.IsFailure)
            return cancelResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}