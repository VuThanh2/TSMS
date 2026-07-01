using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Enrollments.GetMyEnrollments;

// Student xem danh sách toàn bộ Course đã đăng ký kèm điểm số nếu có.
// StudentId được lấy từ JWT token tại Presentation Layer.
public sealed record GetMyEnrollmentsQuery(
    Guid StudentId,
    int Page,
    int PageSize) : IRequest<Result<PagedList<GetMyEnrollmentsOutputDto>>>;

public sealed class GetMyEnrollmentsQueryHandler
    : IRequestHandler<GetMyEnrollmentsQuery, Result<PagedList<GetMyEnrollmentsOutputDto>>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;

    public GetMyEnrollmentsQueryHandler(
        IEnrollmentRepository enrollmentRepository,
        ICourseEnrollmentService courseEnrollmentService) {
        _enrollmentRepository = enrollmentRepository;
        _courseEnrollmentService = courseEnrollmentService;
    }

    public async Task<Result<PagedList<GetMyEnrollmentsOutputDto>>> Handle(
        GetMyEnrollmentsQuery request,
        CancellationToken cancellationToken) {
        var enrollments = await _enrollmentRepository.GetByStudentIdAsync(
            request.StudentId, cancellationToken);

        if (enrollments.Count == 0)
            return Result.Success(PagedList<GetMyEnrollmentsOutputDto>.Create(
                [], request.Page, request.PageSize, 0));

        var courseIds = enrollments.Select(e => e.CourseId).ToList();
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            courseIds, cancellationToken);
        var courseMap = courses.ToDictionary(c => c.CourseId);

        var dtos = enrollments
            .Select(e => EnrollmentMapper.ToGetMyEnrollmentsOutputDto(
                e,
                courseMap.TryGetValue(e.CourseId, out var c) ? c.CourseName : string.Empty))
            .ToList();

        var paged = PagedList<GetMyEnrollmentsOutputDto>.Create(
            dtos, request.Page, request.PageSize, dtos.Count);

        return Result.Success(paged);
    }
}