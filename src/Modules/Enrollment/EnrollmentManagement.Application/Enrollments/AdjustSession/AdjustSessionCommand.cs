using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Enrollments.AdjustSession;

// StudentId được lấy từ JWT token tại Presentation Layer.
// SessionIds: [oldSessionId, newSessionId] — 2 phần tử, thứ tự quan trọng.
public sealed record AdjustSessionCommand(
    Guid EnrollmentId,
    Guid StudentId,
    Guid OldSessionId,
    Guid NewSessionId) : IRequest<Result<AdjustSessionOutputDto>>;

public sealed class AdjustSessionCommandHandler
    : IRequestHandler<AdjustSessionCommand, Result<AdjustSessionOutputDto>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustSessionCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IUnitOfWork unitOfWork) {
        _enrollmentRepository = enrollmentRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdjustSessionOutputDto>> Handle(
        AdjustSessionCommand request,
        CancellationToken cancellationToken) {
        // Precondition 1: Enrollment phải tồn tại và thuộc Student này.
        var enrollment = await _enrollmentRepository.GetByIdAsync(
            request.EnrollmentId, cancellationToken);

        if (enrollment is null || enrollment.StudentId != request.StudentId)
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.NotFound);

        // Precondition 2: Course không được ở trạng thái Completed.
        var courseStatus = await _courseEnrollmentService.GetStatusAsync(
            enrollment.CourseId, cancellationToken);

        if (courseStatus == "Completed")
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.CourseAlreadyCompleted);

        // Precondition 3: NewSessionId phải thuộc Course này, lấy SessionType.
        var allSessions = await _courseEnrollmentService.GetClassSessionsAsync(
            enrollment.CourseId, cancellationToken);

        var newSessionLookup = allSessions.FirstOrDefault(s => s.ClassSessionId == request.NewSessionId);

        if (newSessionLookup is null)
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.SessionNotInCourse);

        if (!Enum.TryParse<SessionType>(newSessionLookup.SessionType, out var newSessionType))
            return Result.Failure<AdjustSessionOutputDto>(EnrollmentErrors.SessionNotInCourse);

        // Domain behaviour — guards bên trong: oldSession tồn tại, không trùng newSession,
        // không trùng SessionType với session còn lại.
        var adjustResult = enrollment.AdjustSession(
            request.OldSessionId,
            request.NewSessionId,
            newSessionType);

        if (adjustResult.IsFailure)
            return Result.Failure<AdjustSessionOutputDto>(adjustResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(EnrollmentMapper.ToAdjustSessionOutputDto(enrollment, allSessions));
    }
}