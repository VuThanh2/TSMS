using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Services;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentAggregate = EnrollmentManagement.Domain.Entities.Enrollment;

namespace Enrollment.UnitTests;

// Business rule quan trọng và dễ hiểu lầm nhất trong Enrollment BC: trùng lịch cần CẢ HAI vế —
//   (1) khoảng ngày của 2 Course GIAO NHAU, và
//   (2) cùng (DayOfWeek, SessionType).
// Chỉ có vế (2) là chặn nhầm: học "Thứ Hai Sáng" kỳ trước rồi lại đăng ký "Thứ Hai Sáng" kỳ này
// thì không đụng nhau chút nào. Chỉ có vế (1) cũng chặn nhầm: 2 Course cùng kỳ nhưng khác ca là
// bình thường. Đây là cùng công thức với check lịch dạy Lecturer (HasLecturerSlotConflictAsync).
//
// So sánh theo DayOfWeek chứ không phải SessionDate cụ thể, vì lịch LẶP LẠI HÀNG TUẦN cả kỳ.
public class ScheduleConflictCheckerTests {
    // Course đang xét đăng ký: tháng 4 → tháng 6.
    private static readonly DateOnly CandidateStart = new(2026, 4, 1);
    private static readonly DateOnly CandidateEnd = new(2026, 6, 30);

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

    // Course chạy CÙNG KỲ với course đang xét (tháng 3 → tháng 5, giao với tháng 4-6).
    private static CourseLookup OverlappingCourse(Guid courseId) =>
        new(courseId, "Existing Course", Guid.NewGuid(), "Upcoming",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 31));

    // Course đã chạy xong TRƯỚC khi course đang xét bắt đầu (tháng 1 → tháng 3).
    private static CourseLookup PastCourse(Guid courseId) =>
        new(courseId, "Last Term Course", Guid.NewGuid(), "Completed",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

    [Fact]
    public async Task HasConflictAsync_StudentHasNoOtherEnrollments_ReturnsFalse() {
        var checker = new ScheduleConflictChecker(
            new FakeEnrollmentRepository([]),
            new FakeCourseEnrollmentService([]));

        var hasConflict = await checker.HasConflictAsync(
            studentId: Guid.NewGuid(),
            excludeCourseId: Guid.NewGuid(),
            candidateStartDate: CandidateStart,
            candidateEndDate: CandidateEnd,
            candidateSlots: [(DayOfWeek.Monday, "Morning")]);

        Assert.False(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_SameWeekdayAndSessionType_OverlappingCourseDates_ReturnsTrue() {
        // Xung đột THẬT: 2 Course chạy cùng kỳ và Student đã giữ chỗ đúng Thứ Hai Sáng.
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
            ]) {
                Courses = [OverlappingCourse(otherCourseId)]
            });

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: Guid.NewGuid(),
            candidateStartDate: CandidateStart,
            candidateEndDate: CandidateEnd,
            candidateSlots: [(DayOfWeek.Monday, "Morning")]);

        Assert.True(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_SameWeekdayAndSessionType_NonOverlappingCourseDates_ReturnsFalse() {
        // Cùng "Thứ Hai Sáng" nhưng Course cũ đã kết thúc 31/3, Course mới bắt đầu 1/4 — hai lớp
        // không bao giờ diễn ra cùng lúc nên KHÔNG phải trùng lịch.
        //
        // Trước đây hàm này trả về true (test cũ tên ...ConflictsEvenWithNonOverlappingCourseDates
        // ghi nhận điều đó như hiện trạng). Đó là hệ quả của việc chỉ so (DayOfWeek, SessionType)
        // mà bỏ qua khoảng ngày, khiến ca của MỌI Course cũ chiếm chỗ vĩnh viễn.
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
            ]) {
                Courses = [PastCourse(otherCourseId)]
            });

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: Guid.NewGuid(),
            candidateStartDate: CandidateStart,
            candidateEndDate: CandidateEnd,
            candidateSlots: [(DayOfWeek.Monday, "Morning")]);

        Assert.False(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_CandidateMatchesUnselectedSlotOfOtherCourse_ReturnsFalse() {
        // Course khác có 2 WeeklySlot (Thứ Hai Sáng + Thứ Tư Chiều) nhưng Student chỉ CHỌN
        // Thứ Hai Sáng khi enroll. Thứ Tư Chiều KHÔNG được Student dùng nên không tính là "occupied",
        // dù nó vẫn tồn tại trong danh sách WeeklySlot của course đó.
        // Course cố tình để CÙNG KỲ, để lý do trả về false chỉ có thể là "slot không được chọn".
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
            ]) {
                Courses = [OverlappingCourse(otherCourseId)]
            });

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: Guid.NewGuid(),
            candidateStartDate: CandidateStart,
            candidateEndDate: CandidateEnd,
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
            ]) {
                Courses = [OverlappingCourse(courseId)]
            });

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: courseId,
            candidateStartDate: CandidateStart,
            candidateEndDate: CandidateEnd,
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
            ]) {
                Courses = [OverlappingCourse(otherCourseId)]
            });

        var hasConflict = await checker.HasConflictAsync(
            studentId: studentId,
            excludeCourseId: Guid.NewGuid(),
            candidateStartDate: CandidateStart,
            candidateEndDate: CandidateEnd,
            candidateSlots: [(DayOfWeek.Friday, "Afternoon")]);

        Assert.False(hasConflict);
    }
}
