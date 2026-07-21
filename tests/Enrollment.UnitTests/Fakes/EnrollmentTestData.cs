using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentAggregate = EnrollmentManagement.Domain.Entities.Enrollment;

namespace Enrollment.UnitTests.Fakes;

// Factory dùng chung cho các test handler — dựng 1 Enrollment aggregate hợp lệ (2 slot) mà không
// phải lặp lại toàn bộ tham số enrich ở mỗi test.
public static class EnrollmentTestData {
    public static EnrollmentAggregate CreateEnrollment(
        Guid studentId,
        Guid courseId,
        Guid slotMorning,
        Guid slotAfternoon,
        string courseStatus = "Active") {
        return EnrollmentAggregate.Create(
            studentId: studentId,
            courseId: courseId,
            slots: [(slotMorning, SessionType.Morning), (slotAfternoon, SessionType.Afternoon)],
            studentFullName: "Nguyen Van A",
            studentEmail: "a.nguyen@example.com",
            courseName: "Test Course",
            courseStatus: courseStatus,
            totalSessionsInCourse: 20).Value;
    }
}
