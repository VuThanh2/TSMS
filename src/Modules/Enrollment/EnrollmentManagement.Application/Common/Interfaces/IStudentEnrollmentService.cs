namespace EnrollmentManagement.Application.Common.Interfaces;

// Cross-BC contract — Enrollment BC queries Identity BC về Student qua interface này.
// Implement bởi Identity.Infrastructure.Services.StudentEnrollmentService.
public interface IStudentEnrollmentService {
    // Check user có tồn tại, đang Active và có role Student không.
    Task<bool> IsActiveStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    // Lấy FullName của Student — enrich response DTO.
    Task<string?> GetFullNameAsync(Guid studentId, CancellationToken cancellationToken = default);

    // Lấy Email của Student — enrich GetCourseEnrollments response cho Lecturer.
    Task<string?> GetEmailAsync(Guid studentId, CancellationToken cancellationToken = default);

    // Batch lookup — tránh N+1 khi 1 session có nhiều Student
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
        IReadOnlyList<Guid> studentIds,
        CancellationToken cancellationToken = default);

    /// Trả về Id của TẤT CẢ Student đang Active
    Task<IReadOnlyList<Guid>> GetActiveStudentIdsAsync(CancellationToken cancellationToken = default);
}