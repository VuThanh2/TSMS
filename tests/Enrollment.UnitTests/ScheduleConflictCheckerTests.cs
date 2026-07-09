using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Services;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentAggregate = EnrollmentManagement.Domain.Entities.Enrollment;

namespace Enrollment.UnitTests;

// Business rule quan trọng và dễ hiểu lầm nhất trong Enrollment BC: trùng lịch được xác định
// theo (DayOfWeek, SessionType) LẶP LẠI HÀNG TUẦN — KHÔNG phải theo khoảng ngày thực tế của
// Course. Nghĩa là 2 Course không hề chạy cùng thời điểm (1 course kết thúc tháng 3, course kia
// bắt đầu tháng 4) nhưng cùng "Thứ Hai buổi Sáng" vẫn bị coi là TRÙNG LỊCH theo thiết kế hiện tại.
public class ScheduleConflictCheckerTests {
    private static EnrollmentAggregate CreateEnrollment(
        Guid studentId, Guid courseId, Guid slotA, Guid slotB) {
        return EnrollmentAggregate.Create(
            studentId: studentId,
            courseId: courseId,
            slots: [(slotA, SessionType.Morning), (slotB, SessionType.Afternoon)],
            studentFullName: "Tran Thi B",
            studentEmail: "b.tran@example.com",
            courseName: "Existing Course",
            courseStatus: "Upcoming",
            totalSessionsInCourse: 20).Value;
    }

    [Fact]
    public async Task HasConflictAsync_StudentHasNoOtherEnrollments_ReturnsFalse() {
        var checker = new ScheduleConflictChecker(
            new FakeEnrollmentRepository([]),
            new FakeCourseEnrollmentService([]));

        var hasConflict = await checker.HasConflictAsync(
            studentId: Guid.NewGuid(),
            excludeCourseId: Guid.NewGuid(),
            candidateSlots: [(DayOfWeek.Monday, "Morning")]);

        Assert.False(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_SameWeekdayAndSessionType_ConflictsEvenWithNonOverlappingCourseDates() {
        // Course cũ (đã enroll) học Thứ Hai buổi Sáng — dù course mới không hề trùng thời gian
        // thực tế với course cũ, rule hiện tại vẫn coi đây là trùng lịch (theo comment code gốc).
        var studentId = Guid.NewGuid();
        var otherCourseId = Guid.NewGuid();
        var occupiedSlotId = Guid.NewGuid();
        var otherSlotId = Guid.NewGuid();

        var existingEnrollment = CreateEnrollment(studentId, otherCourseId, occupiedSlotId, otherSlotId);

        var checker = new ScheduleConflictChecker(
            new FakeEnrollmentRepository([existingEnrollment]),
            new FakeCourseEnrollmentService([
                new WeeklySlotLookup(occupiedSlotId, otherCourseId, "Monday", "Morning"),
                new WeeklySlotLookup(otherSlotId, otherCourseId, "Wednesday", "Afternoon")
            ]));

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: Guid.NewGuid(), // course mới, khác hẳn otherCourseId
            candidateSlots: [(DayOfWeek.Monday, "Morning")]);

        Assert.True(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_CandidateMatchesUnselectedSlotOfOtherCourse_ReturnsFalse() {
        // Course khác có 2 WeeklySlot (Thứ Hai Sáng + Thứ Tư Chiều) nhưng Student chỉ CHỌN
        // Thứ Hai Sáng khi enroll. Thứ Tư Chiều KHÔNG được Student dùng nên không tính là "occupied",
        // dù nó vẫn tồn tại trong danh sách WeeklySlot của course đó.
        var studentId = Guid.NewGuid();
        var otherCourseId = Guid.NewGuid();
        var occupiedSlotId = Guid.NewGuid(); // Student chọn slot này (Monday Morning)
        var unselectedSlotId = Guid.NewGuid(); // Student KHÔNG chọn slot này (Wednesday Afternoon)

        var existingEnrollment = CreateEnrollment(studentId, otherCourseId, occupiedSlotId, Guid.NewGuid());

        var checker = new ScheduleConflictChecker(
            new FakeEnrollmentRepository([existingEnrollment]),
            new FakeCourseEnrollmentService([
                new WeeklySlotLookup(occupiedSlotId, otherCourseId, "Monday", "Morning"),
                new WeeklySlotLookup(unselectedSlotId, otherCourseId, "Wednesday", "Afternoon")
            ]));

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: Guid.NewGuid(),
            candidateSlots: [(DayOfWeek.Wednesday, "Afternoon")]);

        Assert.False(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_ExcludeCourseId_ExcludesStudentsOwnCourseFromConflictPool() {
        // Kịch bản AdjustSession: check trùng lịch cho candidate slot MỚI của CHÍNH course đang
        // enroll — phải loại trừ course đó khỏi enrollment pool, nếu không sẽ luôn báo trùng
        // với chính lịch cũ của mình.
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var slotA = Guid.NewGuid();
        var slotB = Guid.NewGuid();

        var ownEnrollment = CreateEnrollment(studentId, courseId, slotA, slotB);

        var checker = new ScheduleConflictChecker(
            new FakeEnrollmentRepository([ownEnrollment]),
            new FakeCourseEnrollmentService([
                new WeeklySlotLookup(slotA, courseId, "Monday", "Morning"),
                new WeeklySlotLookup(slotB, courseId, "Wednesday", "Afternoon")
            ]));

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: courseId,
            candidateSlots: [(DayOfWeek.Monday, "Morning")]);

        Assert.False(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_CandidateDoesNotMatchAnyOccupiedSlot_ReturnsFalse() {
        var studentId = Guid.NewGuid();
        var otherCourseId = Guid.NewGuid();
        var occupiedSlotId = Guid.NewGuid();

        var existingEnrollment = CreateEnrollment(studentId, otherCourseId, occupiedSlotId, Guid.NewGuid());

        var checker = new ScheduleConflictChecker(
            new FakeEnrollmentRepository([existingEnrollment]),
            new FakeCourseEnrollmentService([
                new WeeklySlotLookup(occupiedSlotId, otherCourseId, "Monday", "Morning")
            ]));

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: Guid.NewGuid(),
            candidateSlots: [(DayOfWeek.Friday, "Afternoon")]);

        Assert.False(hasConflict);
    }
}
