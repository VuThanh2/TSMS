using EnrollmentManagement.Domain.Errors;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentAggregate = EnrollmentManagement.Domain.Entities.Enrollment;

namespace Enrollment.UnitTests;

// Test thuần domain cho Enrollment aggregate + Grade value object — các rule cốt lõi nhất
// của luồng "Student đăng ký học + Lecturer chấm điểm".
public class EnrollmentDomainTests {
    private static EnrollmentAggregate CreateEnrollment(Guid? slotA = null, Guid? slotB = null) =>
        EnrollmentAggregate.Create(
            studentId: Guid.NewGuid(),
            courseId: Guid.NewGuid(),
            slots: [
                (slotA ?? Guid.NewGuid(), SessionType.Morning),
                (slotB ?? Guid.NewGuid(), SessionType.Afternoon)
            ],
            studentFullName: "Tran Thi B",
            studentEmail: "b.tran@example.com",
            courseName: "OOP Advanced",
            courseStatus: "Upcoming",
            totalSessionsInCourse: 20).Value;

    // ── Enrollment.Create

    [Fact]
    public void Create_WithFewerThanTwoSlots_Fails() {
        var result = EnrollmentAggregate.Create(
            studentId: Guid.NewGuid(),
            courseId: Guid.NewGuid(),
            slots: [(Guid.NewGuid(), SessionType.Morning)],
            studentFullName: "Tran Thi B",
            studentEmail: "b.tran@example.com",
            courseName: "OOP Advanced",
            courseStatus: "Upcoming",
            totalSessionsInCourse: 20);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.InvalidSessionCount, result.Error);
    }

    [Fact]
    public void Create_WithDuplicateWeeklySlotId_Fails() {
        var sameSlot = Guid.NewGuid();
        var result = EnrollmentAggregate.Create(
            studentId: Guid.NewGuid(),
            courseId: Guid.NewGuid(),
            slots: [(sameSlot, SessionType.Morning), (sameSlot, SessionType.Afternoon)],
            studentFullName: "Tran Thi B",
            studentEmail: "b.tran@example.com",
            courseName: "OOP Advanced",
            courseStatus: "Upcoming",
            totalSessionsInCourse: 20);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.DuplicateSession, result.Error);
    }

    // ── AssignGrade / UpdateGrade state machine

    [Fact]
    public void AssignGrade_FirstTime_Succeeds() {
        var enrollment = CreateEnrollment();
        var grade = Grade.Create(8.5m).Value;

        var result = enrollment.AssignGrade(grade);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnrollmentStatus.Graded, enrollment.Status);
        Assert.Equal(8.5m, enrollment.Grade!.Value);
    }

    [Fact]
    public void AssignGrade_WhenAlreadyGraded_Fails() {
        var enrollment = CreateEnrollment();
        enrollment.AssignGrade(Grade.Create(8.5m).Value);

        var result = enrollment.AssignGrade(Grade.Create(9.0m).Value);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.AlreadyGraded, result.Error);
        Assert.Equal(8.5m, enrollment.Grade!.Value); // điểm cũ không bị ghi đè
    }

    [Fact]
    public void UpdateGrade_WhenNotYetGraded_Fails() {
        var enrollment = CreateEnrollment();

        var result = enrollment.UpdateGrade(Grade.Create(9.0m).Value);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.NotYetGraded, result.Error);
    }

    [Fact]
    public void UpdateGrade_AfterAssigned_Succeeds() {
        var enrollment = CreateEnrollment();
        enrollment.AssignGrade(Grade.Create(8.5m).Value);

        var result = enrollment.UpdateGrade(Grade.Create(9.5m).Value);

        Assert.True(result.IsSuccess);
        Assert.Equal(9.5m, enrollment.Grade!.Value);
    }

    // ── AdjustSession — ordered guards

    [Fact]
    public void AdjustSession_ToSameSlotItAlreadyHas_Fails() {
        var slotA = Guid.NewGuid();
        var enrollment = CreateEnrollment(slotA: slotA);

        var result = enrollment.AdjustSession(slotA, slotA, SessionType.Morning);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.SessionAlreadyEnrolled, result.Error);
    }

    [Fact]
    public void AdjustSession_OldSlotNotFoundInEnrollment_Fails() {
        var enrollment = CreateEnrollment();

        var result = enrollment.AdjustSession(Guid.NewGuid(), Guid.NewGuid(), SessionType.Morning);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.AdjustSessionNotFound, result.Error);
    }

    [Fact]
    public void AdjustSession_NewSlotEqualsTheOtherEnrolledSlot_Fails() {
        var slotA = Guid.NewGuid();
        var slotB = Guid.NewGuid();
        var enrollment = CreateEnrollment(slotA, slotB);

        // Cố đổi slotA thành đúng slotB (slot kia đang giữ) → phải fail dù != oldWeeklySlotId.
        var result = enrollment.AdjustSession(slotA, slotB, SessionType.Afternoon);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.SessionAlreadyEnrolled, result.Error);
    }

    [Fact]
    public void AdjustSession_NewSessionTypeCollidesWithOtherSlot_Fails() {
        var slotA = Guid.NewGuid();
        var slotB = Guid.NewGuid();
        // slotA = Morning, slotB = Afternoon (theo CreateEnrollment).
        var enrollment = CreateEnrollment(slotA, slotB);

        // Đổi slotA sang 1 slot mới nhưng lại chọn SessionType = Afternoon → trùng slotB.
        var result = enrollment.AdjustSession(slotA, Guid.NewGuid(), SessionType.Afternoon);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.AdjustSessionTypeDuplicate, result.Error);
    }

    [Fact]
    public void AdjustSession_ValidNewSlotAndType_Succeeds() {
        var slotA = Guid.NewGuid();
        var slotB = Guid.NewGuid();
        var enrollment = CreateEnrollment(slotA, slotB);
        var newSlot = Guid.NewGuid();

        var result = enrollment.AdjustSession(slotA, newSlot, SessionType.Morning);

        Assert.True(result.IsSuccess);
        Assert.Contains(enrollment.EnrolledSessions, s => s.WeeklySlotId == newSlot);
        Assert.DoesNotContain(enrollment.EnrolledSessions, s => s.WeeklySlotId == slotA);
    }

    // ── Attendance.Mark idempotency

    [Fact]
    public void Attendance_Mark_SameStatusTwice_IsIdempotent_DoesNotBumpTimestampOrRaiseEvent() {
        var attendance = EnrollmentManagement.Domain.Entities.Attendance.CreateDefault(
            studentId: Guid.NewGuid(), classSessionId: Guid.NewGuid(), courseId: Guid.NewGuid());
        attendance.Mark(AttendanceStatus.Present);
        attendance.ClearDomainEvents();
        var timestampAfterFirstMark = attendance.UpdatedAt;

        var result = attendance.Mark(AttendanceStatus.Present);

        Assert.True(result.IsSuccess);
        Assert.Equal(timestampAfterFirstMark, attendance.UpdatedAt);
        Assert.Empty(attendance.DomainEvents);
    }

    [Fact]
    public void Attendance_Mark_DifferentStatus_UpdatesTimestampAndRaisesEvent() {
        var attendance = EnrollmentManagement.Domain.Entities.Attendance.CreateDefault(
            studentId: Guid.NewGuid(), classSessionId: Guid.NewGuid(), courseId: Guid.NewGuid());
        attendance.ClearDomainEvents();

        var result = attendance.Mark(AttendanceStatus.Present);

        Assert.True(result.IsSuccess);
        Assert.Equal(AttendanceStatus.Present, attendance.Status);
        Assert.Single(attendance.DomainEvents);
    }

    // ── Grade value object

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(5.5)]
    public void Grade_Create_WithinRange_Succeeds(decimal value) {
        var result = Grade.Create(value);
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(10.01)]
    public void Grade_Create_OutOfRange_Fails(decimal value) {
        var result = Grade.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrors.GradeOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(7.125, 7.13)]  // away-from-zero: .5 luôn làm tròn lên
    [InlineData(7.124, 7.12)]
    [InlineData(9.995, 10.00)]
    public void Grade_Create_RoundsToTwoDecimalPlaces_AwayFromZero(decimal input, decimal expected) {
        var result = Grade.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }
}
