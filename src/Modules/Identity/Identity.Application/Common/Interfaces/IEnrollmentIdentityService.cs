namespace Identity.Application.Common.Interfaces;

// Cross-BC interface
// Dùng để check precondition trước khi deactivate Student 
public interface IEnrollmentIdentityService {
    // Trả true nếu Student đang có Enrollment trong ít nhất 1 Course ở trạng thái Active.
    Task<IReadOnlyList<Guid>> GetActiveCourseIdsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}