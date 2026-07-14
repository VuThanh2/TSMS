namespace Reporting.Application.Common.Interfaces;

/// CHỈ dùng cho Demo Data Reset. Vì Course/Enrollment reset dùng bulk delete (không raise domain
/// event), Reporting sẽ không tự nhận event dọn projection như luồng bình thường — phải xóa
/// trực tiếp cả 5 ReadModel ở đây.
public interface IReportingDataResetter {
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}