namespace Enrollment.Application.Common.Interfaces;

// Cross-BC contract — Enrollment BC queries Identity BC về Student qua interface này.
public interface IStudentEnrollmentService {
    // Check user có tồn tại, đang Active và có role Student không.
    Task<bool> IsActiveStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    // Lấy FullName của Student — enrich response DTO.
    Task<string?> GetFullNameAsync(Guid studentId, CancellationToken cancellationToken = default);
}