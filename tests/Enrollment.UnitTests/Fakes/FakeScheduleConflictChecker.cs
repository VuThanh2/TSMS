using EnrollmentManagement.Application.Common.Interfaces;

namespace Enrollment.UnitTests.Fakes;

// Fake IScheduleConflictChecker — logic thật của checker đã được cover riêng trong
// ScheduleConflictCheckerTests; ở test handler ta chỉ cần điều khiển kết quả true/false để
// kiểm nhánh xử lý ScheduleConflict mà không phải dựng lại toàn bộ enrollment pool.
public sealed class FakeScheduleConflictChecker : IScheduleConflictChecker {
    public bool Result { get; set; }
    public int CallCount { get; private set; }

    public Task<bool> HasConflictAsync(
        Guid studentId,
        Guid excludeCourseId,
        DateOnly candidateStartDate,
        DateOnly candidateEndDate,
        IReadOnlyList<(DayOfWeek DayOfWeek, string SessionType)> candidateSlots,
        CancellationToken cancellationToken = default) {
        CallCount++;
        return Task.FromResult(Result);
    }
}
