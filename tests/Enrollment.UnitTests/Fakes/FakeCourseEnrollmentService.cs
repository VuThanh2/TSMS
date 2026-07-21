using EnrollmentManagement.Application.Common.Interfaces;

namespace Enrollment.UnitTests.Fakes;

// Fake cho ICourseEnrollmentService (cross-BC Course→Enrollment). Mỗi nhánh dữ liệu là 1 property
// cấu hình được từ test — mặc định chọn giá trị "đường thông" (Upcoming + Open) để test handler
// chỉ cần override đúng precondition đang muốn kiểm.
public sealed class FakeCourseEnrollmentService : ICourseEnrollmentService {
    private readonly List<WeeklySlotLookup> _weeklySlots;

    // ── Cấu hình precondition EnrollCourse / GradeStudent / AdjustSession
    public bool Upcoming { get; set; } = true;
    public bool OpenForEnrollment { get; set; } = true;
    public int? MaxCapacity { get; set; }
    public string? DefaultStatus { get; set; } = "Upcoming";
    public Dictionary<Guid, string> StatusByCourse { get; set; } = new();

    // ── Dữ liệu tra cứu
    public List<ClassSessionLookup> ClassSessions { get; set; } = new();
    public List<CourseLookup> Courses { get; set; } = new();

    // ── Cấu hình cho SendSessionReminderJobService test
    public List<ClassSessionLookup> ClassSessionsByDate { get; set; } = new();

    // Ghi lại các date mà job thực sự query — dùng để assert job dùng đúng "hôm nay".
    public List<DateOnly> RequestedDates { get; } = new();

    public FakeCourseEnrollmentService(IEnumerable<WeeklySlotLookup>? weeklySlots = null) {
        _weeklySlots = weeklySlots?.ToList() ?? new List<WeeklySlotLookup>();
    }

    public Task<bool> IsUpcomingAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Upcoming);

    public Task<bool> IsOpenForEnrollmentAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(OpenForEnrollment);

    public Task<string?> GetStatusAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(StatusByCourse.TryGetValue(courseId, out var status) ? status : DefaultStatus);

    public Task<int?> GetMaxCapacityAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(MaxCapacity);

    public Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WeeklySlotLookup>>(
            _weeklySlots.Where(s => s.CourseId == courseId).ToList());

    public Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WeeklySlotLookup>>(
            _weeklySlots.Where(s => courseIds.Contains(s.CourseId)).ToList());

    public Task<ClassSessionLookup?> GetClassSessionAsync(
        Guid classSessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ClassSessions.FirstOrDefault(s => s.ClassSessionId == classSessionId));

    public Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsAsync(
        Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClassSessionLookup>>(
            ClassSessions.Where(s => s.CourseId == courseId).ToList());

    public Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByWeeklySlotIdsAsync(
        IReadOnlyList<Guid> weeklySlotIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClassSessionLookup>>(
            ClassSessions.Where(s => weeklySlotIds.Contains(s.WeeklySlotId)).ToList());

    public Task<IReadOnlyList<CourseLookup>> GetCoursesByIdsAsync(
        IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CourseLookup>>(
            Courses.Where(c => courseIds.Contains(c.CourseId)).ToList());

    public Task<IReadOnlyList<CourseLookup>> GetCoursesByLecturerAsync(
        Guid lecturerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CourseLookup>>(
            Courses.Where(c => c.LecturerId == lecturerId).ToList());

    public Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClassSessionLookup>>(
            ClassSessions.Where(s => courseIds.Contains(s.CourseId)).ToList());

    public Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByDateAsync(
        DateOnly date, CancellationToken cancellationToken = default) {
        RequestedDates.Add(date);
        return Task.FromResult<IReadOnlyList<ClassSessionLookup>>(ClassSessionsByDate.ToList());
    }
}
