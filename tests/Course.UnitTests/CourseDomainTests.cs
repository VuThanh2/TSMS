using CourseManagement.Domain.Entities;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.ValueObjects;

namespace Course.UnitTests;

// Test thuần domain (không cần DB) cho các business rule cốt lõi nhất của Course aggregate —
// đây là aggregate trung tâm của cả hệ thống (lịch dạy, sức chứa, vòng đời course).
public class CourseDomainTests {
    // ClassSession.Create là `internal` và SessionDate có `private set` — không có API công khai
    // nào tạo được 1 session "đã qua" (Course luôn generate session tương lai). Dùng reflection
    // để backdate, giống cách UpdateCourseStatusJobServiceTests backdate StartDate/EndDate.
    private static void Backdate(ClassSession session, DateOnly date) {
        typeof(ClassSession).GetProperty(nameof(ClassSession.SessionDate))!.SetValue(session, date);
    }

    private static CourseName Name(string value = "OOP Advanced") => CourseName.Create(value).Value;

    private static DateRange Range(int startOffsetDays = 1, int durationDays = 90) {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(startOffsetDays));
        var end = start.AddDays(durationDays);
        return DateRange.Create(start, end).Value;
    }

    private static CourseManagement.Domain.Entities.Course CreateCourse(
        int maxCapacity = 30, int startOffsetDays = 1, int durationDays = 90) =>
        CourseManagement.Domain.Entities.Course.Create(
            lecturerId: Guid.NewGuid(),
            courseName: Name(),
            description: null,
            dateRange: Range(startOffsetDays, durationDays),
            maxCapacity: maxCapacity,
            lecturerName: "Nguyen Van A").Value;

    // ── DateRange boundary

    [Fact]
    public void DateRange_Create_StartDateExactlyToday_IsAllowed() {
        // Tên lỗi "StartDateMustBeInFuture" gây hiểu lầm — điều kiện thực tế là `startDate < today`,
        // nghĩa là StartDate == today VẪN hợp lệ (today cũng được coi là "tương lai" ở đây).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = DateRange.Create(today, today.AddDays(7));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void DateRange_Create_StartDateYesterday_Fails() {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var result = DateRange.Create(yesterday, yesterday.AddDays(7));

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.StartDateMustBeInFuture, result.Error);
    }

    [Fact]
    public void DateRange_Create_EndDateEqualsStartDate_Fails() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = DateRange.Create(today, today);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.EndDateMustBeAfterStartDate, result.Error);
    }

    // ── Course.Create

    [Fact]
    public void Create_MaxCapacityZeroOrNegative_Fails() {
        var result = CourseManagement.Domain.Entities.Course.Create(
            lecturerId: Guid.NewGuid(),
            courseName: Name(),
            description: null,
            dateRange: Range(),
            maxCapacity: 0,
            lecturerName: "Nguyen Van A");

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.MaxCapacityMustBePositive, result.Error);
    }

    // ── AddWeeklySlot

    [Fact]
    public void AddWeeklySlot_DuplicateDayAndSessionType_Fails() {
        var course = CreateCourse();
        course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning);

        var result = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.DuplicateWeeklySlot, result.Error);
    }

    [Fact]
    public void AddWeeklySlot_SameDayDifferentSessionType_Succeeds() {
        var course = CreateCourse();
        course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning);

        var result = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Afternoon);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, course.WeeklySlots.Count);
    }

    [Fact]
    public void AddWeeklySlot_GeneratesSessionsInclusiveOfEndDate() {
        // Course chạy đúng 7 ngày (1 tuần) — nếu slot trùng DayOfWeek với StartDate,
        // phải sinh đúng 1 session tại StartDate (vì StartDate + 7 ngày > EndDate).
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var end = start.AddDays(6); // đúng 1 tuần, cùng DayOfWeek với start
        var course = CourseManagement.Domain.Entities.Course.Create(
            lecturerId: Guid.NewGuid(),
            courseName: Name(),
            description: null,
            dateRange: DateRange.Create(start, end).Value,
            maxCapacity: 30,
            lecturerName: "Nguyen Van A").Value;

        course.AddWeeklySlot(start.DayOfWeek, SessionType.Morning);

        Assert.Single(course.ClassSessions);
        Assert.Equal(start, course.ClassSessions[0].SessionDate);
    }

    // ── RemoveWeeklySlot — minimum 2 slots invariant

    [Fact]
    public void RemoveWeeklySlot_WhenExactlyTwoSlotsRemain_Fails() {
        var course = CreateCourse();
        var slotA = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);

        var result = course.RemoveWeeklySlot(slotA.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.MinimumWeeklySlotsRequired, result.Error);
        Assert.Equal(2, course.WeeklySlots.Count);
    }

    [Fact]
    public void RemoveWeeklySlot_WhenThreeSlotsExist_SucceedsAndLeavesTwo() {
        var course = CreateCourse();
        var slotA = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        course.AddWeeklySlot(DayOfWeek.Friday, SessionType.Morning);

        var result = course.RemoveWeeklySlot(slotA.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, course.WeeklySlots.Count);
    }

    [Fact]
    public void RemoveWeeklySlot_SoftCancelsFutureSessions_DoesNotDeleteThem() {
        var course = CreateCourse();
        var slotA = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        course.AddWeeklySlot(DayOfWeek.Friday, SessionType.Morning);
        var sessionCountBeforeRemoval = course.ClassSessions.Count;

        course.RemoveWeeklySlot(slotA.Id);

        // Session vẫn còn trong danh sách (soft-cancel), không bị xóa khỏi Course.
        Assert.Equal(sessionCountBeforeRemoval, course.ClassSessions.Count);
        Assert.All(
            course.ClassSessions.Where(s => s.WeeklySlotId == slotA.Id),
            s => Assert.True(s.IsCancelled));
    }

    // ── TransitionStatus — strict forward-only state machine

    [Theory]
    [InlineData(CourseStatus.Upcoming, CourseStatus.Active, true)]
    [InlineData(CourseStatus.Active, CourseStatus.Completed, true)]
    [InlineData(CourseStatus.Upcoming, CourseStatus.Completed, false)] // skip Active
    [InlineData(CourseStatus.Active, CourseStatus.Upcoming, false)]    // reverse
    [InlineData(CourseStatus.Completed, CourseStatus.Active, false)]   // reverse from terminal
    [InlineData(CourseStatus.Upcoming, CourseStatus.Upcoming, false)]  // no-op self-transition
    public void TransitionStatus_OnlyAllowsStrictForwardPath(
        CourseStatus from, CourseStatus to, bool expectedSuccess) {
        var course = CreateCourse();

        // Đưa course về đúng trạng thái "from" bằng transition hợp lệ (Upcoming là mặc định).
        if (from is CourseStatus.Active or CourseStatus.Completed)
            course.TransitionStatus(CourseStatus.Active);
        if (from == CourseStatus.Completed)
            course.TransitionStatus(CourseStatus.Completed);

        var result = course.TransitionStatus(to);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        if (!expectedSuccess)
            Assert.Equal(CourseErrors.InvalidStatusTransition, result.Error);
    }

    // ── UpdateInfo — Completed immutability

    [Fact]
    public void UpdateInfo_WhenCourseCompleted_Fails() {
        var course = CreateCourse();
        course.TransitionStatus(CourseStatus.Active);
        course.TransitionStatus(CourseStatus.Completed);

        var result = course.UpdateInfo(Name("New Name"), null, course.EndDate.AddDays(30), 40);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.CompletedCourseIsImmutable, result.Error);
    }

    [Fact]
    public void UpdateInfo_ExtendingEndDate_GeneratesAdditionalSessionsForExistingSlots() {
        var course = CreateCourse(durationDays: 13); // 2 tuần chẵn
        course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning);
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var sessionCountBeforeExtend = course.ClassSessions.Count;

        var result = course.UpdateInfo(Name(), null, course.EndDate.AddDays(14), course.MaxCapacity);

        Assert.True(result.IsSuccess);
        Assert.True(course.ClassSessions.Count > sessionCountBeforeExtend);
    }

    // ── AddWeeklySlot / RemoveWeeklySlot rejected on Completed course

    [Fact]
    public void AddWeeklySlot_WhenCourseCompleted_Fails() {
        var course = CreateCourse();
        course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning);
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        course.TransitionStatus(CourseStatus.Active);
        course.TransitionStatus(CourseStatus.Completed);

        var result = course.AddWeeklySlot(DayOfWeek.Friday, SessionType.Morning);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.CompletedCourseIsImmutable, result.Error);
    }

    // ── CancelClassSession — Completed immutability
    //
    // Trong luồng bình thường (chỉ Background Job gọi TransitionStatus dựa trên EndDate < today),
    // Status == Completed luôn kéo theo mọi ClassSession đã SessionDate <= EndDate < today, tức
    // IsPast() luôn true — nên thiếu guard Completed ở đây trông như vô hại. NHƯNG TransitionStatus
    // tự nó không hề validate ngày tháng (chỉ là 1 state machine thuần), nên bất kỳ code path nào
    // khác gọi trực tiếp TransitionStatus(Completed) trong khi Course vẫn còn ClassSession tương lai
    // sẽ khiến CancelClassSession vẫn cho phép hủy — vi phạm invariant "Completed course is
    // immutable" mà MỌI mutator khác (UpdateInfo, AddWeeklySlot, RemoveWeeklySlot,
    // UpdateClassSession, ReplaceLecturer) đều enforce. Domain phải tự bảo vệ invariant của
    // chính nó, không được phụ thuộc vào cách caller happens to sử dụng nó.
    [Fact]
    public void CancelClassSession_WhenCourseCompleted_Fails_EvenIfSessionIsStillInTheFuture() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var futureSession = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);

        // Ép Course sang Completed trực tiếp qua state machine (không qua Background Job) —
        // futureSession vẫn còn SessionDate trong tương lai, chưa hề trở thành "past".
        course.TransitionStatus(CourseStatus.Active);
        course.TransitionStatus(CourseStatus.Completed);

        var result = course.CancelClassSession(futureSession.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.CompletedCourseIsImmutable, result.Error);
        Assert.False(futureSession.IsCancelled);
    }

    [Fact]
    public void CancelClassSession_ValidFutureSession_Succeeds() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);

        var result = course.CancelClassSession(session.Id);

        Assert.True(result.IsSuccess);
        Assert.True(session.IsCancelled);
    }

    [Fact]
    public void CancelClassSession_SessionNotFound_Fails() {
        var course = CreateCourse();
        course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning);
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);

        var result = course.CancelClassSession(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.ClassSessionNotFound, result.Error);
    }

    [Fact]
    public void CancelClassSession_AlreadyCancelled_Fails() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);
        course.CancelClassSession(session.Id);

        var result = course.CancelClassSession(session.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.ClassSessionAlreadyCancelled, result.Error);
    }

    [Fact]
    public void CancelClassSession_PastSession_Fails() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);
        Backdate(session, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        var result = course.CancelClassSession(session.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.CannotModifyPastClassSession, result.Error);
    }

    // ── UpdateClassSession — boundary [_startDate, _endDate] phải inclusive cả 2 đầu

    [Fact]
    public void UpdateClassSession_NewDateExactlyAtStartDate_Succeeds() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);

        var result = course.UpdateClassSession(session.Id, course.StartDate, SessionType.Morning);

        Assert.True(result.IsSuccess);
        Assert.Equal(course.StartDate, session.SessionDate);
    }

    [Fact]
    public void UpdateClassSession_NewDateExactlyAtEndDate_Succeeds() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);

        var result = course.UpdateClassSession(session.Id, course.EndDate, SessionType.Morning);

        Assert.True(result.IsSuccess);
        Assert.Equal(course.EndDate, session.SessionDate);
    }

    [Fact]
    public void UpdateClassSession_NewDateOneDayBeforeStartDate_Fails() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);

        var result = course.UpdateClassSession(
            session.Id, course.StartDate.AddDays(-1), SessionType.Morning);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.ClassSessionOutsideDateRange, result.Error);
    }

    [Fact]
    public void UpdateClassSession_NewDateOneDayAfterEndDate_Fails() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);

        var result = course.UpdateClassSession(
            session.Id, course.EndDate.AddDays(1), SessionType.Morning);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.ClassSessionOutsideDateRange, result.Error);
    }

    [Fact]
    public void UpdateClassSession_CollidesWithAnotherExistingSession_Fails() {
        var course = CreateCourse();
        var slotA = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var sessionA = course.ClassSessions.First(s => s.WeeklySlotId == slotA.Id);
        var sessionB = course.ClassSessions.First(s => s.WeeklySlotId != slotA.Id);

        // Dời sessionA sang đúng ngày + loại buổi của sessionB → phải bị chặn trùng.
        var result = course.UpdateClassSession(sessionA.Id, sessionB.SessionDate, sessionB.SessionType);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.DuplicateClassSession, result.Error);
    }

    [Fact]
    public void UpdateClassSession_WhenCourseCompleted_Fails() {
        var course = CreateCourse();
        var slot = course.AddWeeklySlot(DayOfWeek.Monday, SessionType.Morning).Value;
        course.AddWeeklySlot(DayOfWeek.Wednesday, SessionType.Afternoon);
        var session = course.ClassSessions.First(s => s.WeeklySlotId == slot.Id);
        course.TransitionStatus(CourseStatus.Active);
        course.TransitionStatus(CourseStatus.Completed);

        var result = course.UpdateClassSession(session.Id, course.EndDate, SessionType.Morning);

        Assert.True(result.IsFailure);
        Assert.Equal(CourseErrors.CompletedCourseIsImmutable, result.Error);
    }
}
