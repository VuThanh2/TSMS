using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Courses.OpenCourseEnrollment;

// Admin mở cổng đăng ký sau khi đã dựng xong lịch tuần. Trước khi mở, Course vô hình với
// Student (GetAvailableCourses lọc IsOpenForEnrollment) nên Admin còn sửa/xóa thoải mái.
// Sau khi mở, Student enroll được — và Course có người enroll thì không xóa được nữa.
public sealed record OpenCourseEnrollmentCommand(Guid CourseId)
    : IRequest<Result<OpenCourseEnrollmentOutputDto>>;

public sealed class OpenCourseEnrollmentCommandHandler
    : IRequestHandler<OpenCourseEnrollmentCommand, Result<OpenCourseEnrollmentOutputDto>> {
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseUnitOfWork _unitOfWork;

    public OpenCourseEnrollmentCommandHandler(
        ICourseRepository courseRepository,
        ICourseUnitOfWork unitOfWork) {
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OpenCourseEnrollmentOutputDto>> Handle(
        OpenCourseEnrollmentCommand request,
        CancellationToken cancellationToken) {
        // PHẢI load kèm WeeklySlots: OpenEnrollment() đếm _weeklySlots để enforce tối thiểu 2 ca.
        // Dùng GetByIdAsync thường sẽ cho collection rỗng → invariant hiểu nhầm là chưa có slot.
        // Không dùng GetByIdWithSessionsAsync vì không cần đụng tới ClassSession.
        var course = await _courseRepository.GetByIdWithWeeklySlotsAsync(
            request.CourseId, cancellationToken);

        if (course is null)
            return Result.Failure<OpenCourseEnrollmentOutputDto>(CourseErrors.NotFound);

        var openResult = course.OpenEnrollment();
        if (openResult.IsFailure)
            return Result.Failure<OpenCourseEnrollmentOutputDto>(openResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new OpenCourseEnrollmentOutputDto(course.IsOpenForEnrollment));
    }
}
