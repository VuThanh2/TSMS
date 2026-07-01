namespace Identity.Application.Common.Interfaces;

// Cross-BC contract — Identity BC queries Course BC qua interface này.
public interface ICourseLookupService {
    // Trả true nếu Lecturer đang phụ trách ít nhất 1 Course Upcoming hoặc Active.
    Task<bool> HasActiveCoursesByLecturerAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default);

    // Trả true nếu có ít nhất 1 courseId trong danh sách đang Active.
    // Dùng để check Student có enrolled trong Course Active không.
    Task<bool> AreAnyActiveAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default);
}