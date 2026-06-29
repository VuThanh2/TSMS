using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using MediatR;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Grading.GradeStudent;

// LecturerId được lấy từ JWT token tại Presentation Layer.
public sealed record GradeStudentCommand(
    Guid EnrollmentId,
    Guid LecturerId,
    decimal Grade) : IRequest<Result<GradeStudentOutputDto>>;

public sealed class GradeStudentCommandHandler
    : IRequestHandler<GradeStudentCommand, Result<GradeStudentOutputDto>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public GradeStudentCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        ICourseEnrollmentService courseEnrollmentService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork) {
        _enrollmentRepository = enrollmentRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GradeStudentOutputDto>> Handle(
        GradeStudentCommand request,
        CancellationToken cancellationToken) {
        // Precondition 1: Enrollment phải tồn tại.
        var enrollment = await _enrollmentRepository.GetByIdAsync(
            request.EnrollmentId, cancellationToken);

        if (enrollment is null)
            return Result.Failure<GradeStudentOutputDto>(EnrollmentErrors.NotFound);

        // Precondition 2: Lecturer phải là người phụ trách Course này.
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            [enrollment.CourseId], cancellationToken);

        var course = courses.FirstOrDefault(c => c.CourseId == enrollment.CourseId);

        if (course is null || course.LecturerId != request.LecturerId)
            return Result.Failure<GradeStudentOutputDto>(EnrollmentErrors.NotCourseOwner);

        // Precondition 3: Course phải đang Active hoặc Completed.
        var courseStatus = await _courseEnrollmentService.GetStatusAsync(
            enrollment.CourseId, cancellationToken);

        if (courseStatus is not ("Active" or "Completed"))
            return Result.Failure<GradeStudentOutputDto>(EnrollmentErrors.CourseNotGradeable);

        // Domain Value Object — validate range [0.00, 10.00].
        var gradeResult = Grade.Create(request.Grade);
        if (gradeResult.IsFailure)
            return Result.Failure<GradeStudentOutputDto>(gradeResult.Error);

        // Domain behaviour — phân biệt lần đầu vs cập nhật.
        var gradeOperationResult = enrollment.Status == EnrollmentStatus.Graded
            ? enrollment.UpdateGrade(gradeResult.Value)
            : enrollment.AssignGrade(gradeResult.Value);

        if (gradeOperationResult.IsFailure)
            return Result.Failure<GradeStudentOutputDto>(gradeOperationResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fire-and-forget: thông báo real-time qua SignalR — không block kết quả.
        _ = _notificationService.NotifyGradeUpdatedAsync(
            enrollment.StudentId,
            enrollment.CourseId,
            course.CourseName,
            gradeResult.Value.Value,
            CancellationToken.None);

        return Result.Success(EnrollmentMapper.ToGradeStudentOutputDto(enrollment));
    }
}