namespace Identity.Application.Common.Interfaces;

// Cross-BC interface
// Enrollment BC reference Identity.Application để inject interface này
public interface IUserQueryService {
    // Kiểm tra user có tồn tại không (bất kể isActive).
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Kiểm tra user có đang active không.
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    // Kiểm tra user có role cụ thể không.
    Task<bool> HasRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    Task<string?> GetFullNameAsync(Guid userId, CancellationToken cancellationToken = default);
}