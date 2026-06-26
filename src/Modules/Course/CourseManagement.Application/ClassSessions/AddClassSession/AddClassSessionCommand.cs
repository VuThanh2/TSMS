using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Application.ClassSessions.AddClassSession;

public sealed record AddClassSessionCommand(
    Guid CourseId,
    DateOnly SessionDate,
    string SessionType) : IRequest<Result<AddClassSessionOutputDto>>;

public sealed class AddClassSessionCommandHandler
    : IRequestHandler<AddClassSessionCommand, Result<AddClassSessionOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddClassSessionCommandHandler(
        ICourseRepository courseRepository,
        IUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddClassSessionOutputDto>> Handle(
        AddClassSessionCommand request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithSessionsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<AddClassSessionOutputDto>(CourseErrors.NotFound);

        if (!Enum.TryParse<SessionType>(request.SessionType, ignoreCase: true, out var sessionType))
            return Result.Failure<AddClassSessionOutputDto>(
                Error.Create("Course.InvalidSessionType",
                    "SessionType must be 'Morning' or 'Afternoon'."));

        // Domain enforces: Completed immutable, date trong range, no duplicate.
        var addResult = course.AddClassSession(request.SessionDate, sessionType);

        if (addResult.IsFailure)
            return Result.Failure<AddClassSessionOutputDto>(addResult.Error);

        _courseRepository.Update(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var session = addResult.Value;

        return Result.Success(new AddClassSessionOutputDto(
            ClassSessionId: session.Id,
            CourseId: session.CourseId,
            SessionDate: session.SessionDate,
            DayOfWeek: session.DayOfWeek.ToString(),
            SessionType: session.SessionType.ToString(),
            IsPast: session.IsPast()));
    }
}