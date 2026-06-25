namespace CourseManagement.Application.Common.Interfaces;

/// Cross-BC contract — Course BC queries Identity BC through this interface.
public interface ILecturerLookupService {
    /// Returns true if the user exists and is currently Active with Lecturer role.
    Task<bool> IsActiveLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);

    Task<string?> GetFullNameAsync(Guid lecturerId, CancellationToken cancellationToken = default);
}