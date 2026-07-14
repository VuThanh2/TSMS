namespace CourseManagement.Application.Common.Interfaces;

/// CHỈ dùng cho Demo Data Reset (gọi từ ResetDemoCourseDataCommand). Xóa toàn bộ
/// Course/WeeklySlot/ClassSession bằng bulk delete trực tiếp
public interface ICourseDataResetter {
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}