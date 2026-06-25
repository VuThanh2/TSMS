namespace Identity.Application.Common.Interfaces;

// Cross-BC interface
// Dùng để check precondition trước khi deactivate Student 
public interface IEnrollmentLookupService {
    // Trả true nếu Student đang có Enrollment trong ít nhất 1 Course ở trạng thái Active.
    Task<bool> HasActiveEnrollmentsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}