namespace CourseManagement.Application.Common.Interfaces;

/// Cross-BC contract — Course BC queries Identity BC through this interface.
public interface ILecturerLookupService {
    /// Returns true if the user exists and is currently Active with Lecturer role.
    Task<bool> IsActiveLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);

    Task<string?> GetFullNameAsync(Guid lecturerId, CancellationToken cancellationToken = default);

    /// Trả về Id của TẤT CẢ Lecturer đang Active — dùng cho Demo Data Seeder để round-robin
    /// gán Course mẫu đều cho từng Lecturer (thay vì dồn hết vào 1 người). Danh sách rỗng nếu
    /// chưa có Lecturer nào Active (vd: reset-demo-data gọi trước khi Import CSV).
    Task<IReadOnlyList<Guid>> GetActiveLecturerIdsAsync(CancellationToken cancellationToken = default);
}