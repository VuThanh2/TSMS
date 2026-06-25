namespace Identity.Application.Common.Interfaces;

// Cross-BC interface
// Dùng để check precondition trước khi deactivate Lecturer 
public interface ICourseLookupService {
    // Trả true nếu Lecturer đang phụ trách ít nhất 1 Course ở trạng thái Upcoming hoặc Active.
    Task<bool> HasActiveCoursesByLecturerAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default);
}