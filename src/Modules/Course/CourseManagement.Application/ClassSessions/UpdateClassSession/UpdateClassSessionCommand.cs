using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Application.ClassSessions.UpdateClassSession;

public sealed record UpdateClassSessionCommand(
    Guid CourseId,
    Guid ClassSessionId,
    DateOnly SessionDate,
    string SessionType) : IRequest<Result<UpdateClassSessionOutputDto>>;

public sealed class UpdateClassSessionCommandHandler
    : IRequestHandler<UpdateClassSessionCommand, Result<UpdateClassSessionOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClassSessionCommandHandler(
        ICourseRepository courseRepository,
        IUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UpdateClassSessionOutputDto>> Handle(
        UpdateClassSessionCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<UpdateClassSessionOutputDto>(CourseErrors.NotFound);

        if (!Enum.TryParse<SessionType>(request.SessionType, ignoreCase: true, out var sessionType))
            return Result.Failure<UpdateClassSessionOutputDto>(
                Error.Create("Course.InvalidSessionType",
                    "SessionType must be 'Morning' or 'Afternoon'."));

        // Domain enforces: Completed immutable, session not past, date trong range, no duplicate.
        var updateResult = course.UpdateClassSession(
            request.ClassSessionId,
            request.SessionDate,
            sessionType);

        if (updateResult.IsFailure)
            return Result.Failure<UpdateClassSessionOutputDto>(updateResult.Error);

        _courseRepository.Update(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var session = course.ClassSessions.First(s => s.Id == request.ClassSessionId);

        return Result.Success(new UpdateClassSessionOutputDto(
            ClassSessionId: session.Id,
            CourseId: session.CourseId,
            SessionDate: session.SessionDate,
            DayOfWeek: session.DayOfWeek.ToString(),
            SessionType: session.SessionType.ToString(),
            IsPast: session.IsPast()));
    }
}