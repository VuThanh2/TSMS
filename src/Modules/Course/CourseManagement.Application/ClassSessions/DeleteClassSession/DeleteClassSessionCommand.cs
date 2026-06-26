using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Application.ClassSessions.DeleteClassSession;

public sealed record DeleteClassSessionCommand(
    Guid CourseId,
    Guid ClassSessionId) : IRequest<Result>;

public sealed class DeleteClassSessionCommandHandler
    : IRequestHandler<DeleteClassSessionCommand, Result> {
    private readonly ICourseRepository _courseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClassSessionCommandHandler(
        ICourseRepository courseRepository,
        IUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteClassSessionCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure(CourseErrors.NotFound);

        // Domain enforces: session exists, not past, min 2 sessions must remain.
        var removeResult = course.RemoveClassSession(request.ClassSessionId);

        if (removeResult.IsFailure)
            return removeResult;

        _courseRepository.Update(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}