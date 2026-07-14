using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Mappers;
using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Enrollments.GetCourseEnrollments;

// Lecturer xem danh sách Student đã enroll Course kèm điểm số.
// LecturerId được lấy từ JWT token tại Presentation Layer — dùng để verify course ownership.
public sealed record GetCourseEnrollmentsQuery(
    Guid CourseId,
    Guid LecturerId,
    string? Keyword,
    int Page,
    int PageSize) : IRequest<Result<PagedList<GetCourseEnrollmentsOutputDto>>>;

public sealed class GetCourseEnrollmentsQueryHandler
    : IRequestHandler<GetCourseEnrollmentsQuery, Result<PagedList<GetCourseEnrollmentsOutputDto>>> {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly ICourseEnrollmentService _courseEnrollmentService;

    public GetCourseEnrollmentsQueryHandler(
        IEnrollmentRepository enrollmentRepository,
        IStudentEnrollmentService studentEnrollmentService,
        ICourseEnrollmentService courseEnrollmentService) {
        _enrollmentRepository = enrollmentRepository;
        _studentEnrollmentService = studentEnrollmentService;
        _courseEnrollmentService = courseEnrollmentService;
    }

    public async Task<Result<PagedList<GetCourseEnrollmentsOutputDto>>> Handle(
        GetCourseEnrollmentsQuery request,
        CancellationToken cancellationToken) {
        // Precondition: Lecturer phải là người phụ trách Course này.
        var courses = await _courseEnrollmentService.GetCoursesByIdsAsync(
            [request.CourseId], cancellationToken);
 
        var course = courses.FirstOrDefault(c => c.CourseId == request.CourseId);
 
        if (course is null || course.LecturerId != request.LecturerId)
            return Result.Failure<PagedList<GetCourseEnrollmentsOutputDto>>(
                EnrollmentErrors.NotCourseOwner);
 
        var enrollments = await _enrollmentRepository.GetByCourseIdAsync(
            request.CourseId, cancellationToken);
 
        var dtos = new List<GetCourseEnrollmentsOutputDto>(enrollments.Count);
 
        foreach (var enrollment in enrollments) {
            var fullName = await _studentEnrollmentService.GetFullNameAsync(
                enrollment.StudentId, cancellationToken);
            var email = await _studentEnrollmentService.GetEmailAsync(
                enrollment.StudentId, cancellationToken);
 
            dtos.Add(EnrollmentMapper.ToGetCourseEnrollmentsOutputDto(enrollment, fullName, email));
        }
 
        // Search theo tên hoặc email, bỏ qua hoa/thường LẪN dấu tiếng Việt
        // (gõ "Vu" khớp "Vũ"). Search này chạy in-memory nên dùng TextSearch;
        // không dùng OrdinalIgnoreCase vì nó vẫn phân biệt dấu.
        if (!string.IsNullOrWhiteSpace(request.Keyword)) {
            var keyword = request.Keyword.Trim();

            dtos = dtos.Where(dto =>
                TextSearch.ContainsNormalized(dto.StudentFullName, keyword) ||
                TextSearch.ContainsNormalized(dto.StudentEmail, keyword)
            ).ToList();
        }
 
        var paged = PagedList<GetCourseEnrollmentsOutputDto>.Create(
            dtos, request.Page, request.PageSize, dtos.Count);
 
        return Result.Success(paged);
    }
}