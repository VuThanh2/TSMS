using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Dev.SeedDemoEnrollmentData;

// Input là 2 danh sách CourseId do ResetDemoCourseDataCommand (CourseManagement BC) vừa tạo —
// Enrollment BC không tự biết Course nào Active/Completed, DevController (Composition Root)
// truyền kết quả bước reset Course sang đây.
public sealed record SeedDemoEnrollmentDataCommand(
    IReadOnlyList<DemoEnrollTarget> Targets) : IRequest<Result<SeedDemoEnrollmentDataOutputDto>>;

public sealed class SeedDemoEnrollmentDataCommandHandler
    : IRequestHandler<SeedDemoEnrollmentDataCommand, Result<SeedDemoEnrollmentDataOutputDto>> {
    // Cycle điểm số qua từng Student — để Score Distribution chart có phổ điểm đa dạng
    // thay vì tất cả Student cùng 1 điểm (nhìn giả tạo).
    private static readonly decimal[] GradeCycle = [9.5m, 8.0m, 7.5m, 6.0m, 5.5m, 9.0m, 4.5m, 8.5m];

    // Bảng phân bổ Student → Course, đánh số theo vị trí trong danh sách 6 Course "không-Upcoming"
    // ghép lại theo thứ tự [Completed[0], Completed[1], Completed[2], Active[0], Active[1], Active[2]]
    // (0-2 = Completed, 3-5 = Active).
    // Mỗi Student chỉ học 3/6 Course — mỗi Course có đúng 4/8 Student, mỗi Student có CẢ Completed lẫn Active để Reporting vừa có lịch
    // sử điểm vừa có dữ liệu đang học.
    // PHÂN BỔ ĐỀU THEO NGÀY: 6 Course có ngày Sáng phân biệt và ngày Chiều phân biệt (xem
    // ResetDemoCourseDataCommand) nên mọi tổ hợp đều KHÔNG đụng ca.
    private static readonly int[][] StudentCoursePlan = [
        [0, 1, 4],
        [0, 2, 3],
        [1, 2, 5],
        [0, 1, 5],
        [2, 3, 4],
        [2, 3, 5],
        [0, 3, 4],
        [1, 4, 5],
    ];

    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly IEnrollmentUnitOfWork _unitOfWork;

    public SeedDemoEnrollmentDataCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IStudentEnrollmentService studentEnrollmentService,
        IEnrollmentUnitOfWork unitOfWork) {
        _enrollmentRepository = enrollmentRepository;
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _studentEnrollmentService = studentEnrollmentService;
        _unitOfWork = unitOfWork;
    }

    private sealed class CourseSeedContext {
        public required Guid CourseId { get; init; }
        public required bool IsCompleted { get; init; }
        public required string CourseName { get; init; }
        public required List<(Guid WeeklySlotId, SessionType SessionType)> SlotPairs { get; init; }
        public required IReadOnlyList<ClassSessionLookup> Sessions { get; init; }
        public required int MaxCapacity { get; init; }
        public int EnrolledCount { get; set; }
    }

    public async Task<Result<SeedDemoEnrollmentDataOutputDto>> Handle(
        SeedDemoEnrollmentDataCommand request,
        CancellationToken cancellationToken) {
        // Targets đã theo đúng thứ tự [Completed x3, Active x3] = index 0-5 để khớp StudentCoursePlan.
        var targets = request.Targets;
        if (targets.Count == 0)
            return Result.Success(new SeedDemoEnrollmentDataOutputDto(0, 0));

        // Chưa Import CSV tạo Student thì bỏ qua im lặng (không phải lỗi cứng)
        var studentIds = await _studentEnrollmentService.GetActiveStudentIdsAsync(cancellationToken);
        if (studentIds.Count == 0)
            return Result.Success(new SeedDemoEnrollmentDataOutputDto(0, 0));

        // Cache tên/email Student 1 lần — tránh gọi lặp lại cho mỗi Course.
        var studentEmails = await _studentEnrollmentService.GetEmailsAsync(studentIds, cancellationToken);
        var studentFullNames = new Dictionary<Guid, string>();
        foreach (var id in studentIds)
            studentFullNames[id] = await _studentEnrollmentService.GetFullNameAsync(id, cancellationToken)
                                    ?? string.Empty;

        // Chuẩn bị thông tin từng Course 1 lần (không load lại mỗi lần enroll 1 Student).
        var courseIds = targets.Select(t => t.CourseId).ToList();
        var courseLookups = await _courseEnrollmentService.GetCoursesByIdsAsync(courseIds, cancellationToken);
        var courseNames = courseLookups.ToDictionary(c => c.CourseId, c => c.CourseName);
        var allSlots = await _courseEnrollmentService.GetWeeklySlotsByCourseIdsAsync(courseIds, cancellationToken);
        var sessionTypeBySlotId = allSlots.ToDictionary(s => s.WeeklySlotId, s => s.SessionType);

        // courseContexts theo ĐÚNG index của targets (0-5) để StudentCoursePlan tra thẳng. Phần tử
        // null = target lỗi (không nên xảy ra với data vừa seed) → PASS 1 bỏ qua an toàn.
        var courseContexts = new List<CourseSeedContext?>(targets.Count);
        foreach (var target in targets) {
            if (!sessionTypeBySlotId.TryGetValue(target.MorningSlotId, out var morningTypeStr) ||
                !sessionTypeBySlotId.TryGetValue(target.AfternoonSlotId, out var afternoonTypeStr) ||
                !Enum.TryParse<SessionType>(morningTypeStr, out var morningType) ||
                !Enum.TryParse<SessionType>(afternoonTypeStr, out var afternoonType)) {
                courseContexts.Add(null);
                continue;
            }

            var slotPairs = new List<(Guid WeeklySlotId, SessionType SessionType)> {
                (target.MorningSlotId, morningType),
                (target.AfternoonSlotId, afternoonType)
            };
            var courseSessions = await _courseEnrollmentService.GetClassSessionsByWeeklySlotIdsAsync(
                [target.MorningSlotId, target.AfternoonSlotId], cancellationToken);
            var maxCapacity = await _courseEnrollmentService.GetMaxCapacityAsync(target.CourseId, cancellationToken)
                              ?? int.MaxValue;

            courseContexts.Add(new CourseSeedContext {
                CourseId = target.CourseId,
                IsCompleted = target.IsCompleted,
                CourseName = courseNames.GetValueOrDefault(target.CourseId, string.Empty),
                SlotPairs = slotPairs,
                Sessions = courseSessions,
                MaxCapacity = maxCapacity
            });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var enrollmentCount = 0;
        var attendanceCount = 0;

        // Gom Enrollment vừa tạo (kèm ctx + studentIndex) để xử lý Grade/Attendance ở PASS 2.
        var pendingEnrollments = new List<(Enrollment Enrollment, CourseSeedContext Ctx, int StudentIndex)>();

        // ── PASS 1: chỉ tạo Enrollment (raise StudentEnrolledEvent) rồi SaveChanges.
        for (var studentIndex = 0; studentIndex < studentIds.Count; studentIndex++) {
            var studentId = studentIds[studentIndex];
            var plan = StudentCoursePlan[studentIndex % StudentCoursePlan.Length];

            foreach (var courseIndex in plan) {
                // Bound-check phòng khi số Course "không-Upcoming" thực tế khác 6 (vd sau này đổi
                // ResetDemoCourseDataCommand) — bỏ qua thay vì lỗi index-out-of-range.
                if (courseIndex >= courseContexts.Count)
                    continue;

                var ctx = courseContexts[courseIndex];
                if (ctx is null)
                    continue;

                if (ctx.EnrolledCount >= ctx.MaxCapacity)
                    continue; // Đã đầy chỗ — bỏ qua an toàn (không nên xảy ra với plan 4/8 hiện tại).

                var enrollmentResult = Enrollment.Create(
                    studentId,
                    ctx.CourseId,
                    ctx.SlotPairs,
                    studentFullNames.GetValueOrDefault(studentId, string.Empty),
                    studentEmails.GetValueOrDefault(studentId, string.Empty),
                    ctx.CourseName,
                    ctx.IsCompleted ? "Completed" : "Active",
                    totalSessionsInCourse: ctx.Sessions.Count);

                if (enrollmentResult.IsFailure)
                    continue;

                _enrollmentRepository.Add(enrollmentResult.Value);
                enrollmentCount++;
                ctx.EnrolledCount++;
                pendingEnrollments.Add((enrollmentResult.Value, ctx, studentIndex));
            }
        }

        // Flush toàn bộ StudentEnrolledEvent TRƯỚC (OccurredOn sớm hơn hẳn PASS 2).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── PASS 2: Grade (Completed) + Attendance (buổi đã qua). Grade và Attendance độc lập
        // với nhau nên thứ tự tương đối giữa chúng không quan trọng — chỉ cần cả 2 SAU StudentEnrolled.
        foreach (var (enrollment, ctx, studentIndex) in pendingEnrollments) {
            // Completed course → chấm điểm luôn (cycle điểm cho phổ đa dạng).
            // Active course → giữ EnrollmentStatus.Active, chưa chấm (course chưa kết thúc thật).
            if (ctx.IsCompleted) {
                var gradeResult = Grade.Create(GradeCycle[studentIndex % GradeCycle.Length]);
                if (gradeResult.IsSuccess)
                    _ = enrollment.AssignGrade(gradeResult.Value);
            }

            // Attendance pre-populate cho đúng ClassSession thuộc 2 WeeklySlot đã chọn — giống
            // hệt luồng EnrollCourseCommand thật. Buổi ĐÃ QUA được đánh dấu điểm danh luôn (xen
            // 1 Absent mỗi 4 buổi cho phổ điểm danh thực tế, không phải toàn Present tuyệt đối).
            var attendances = new List<Attendance>();
            var pastSessionIndex = 0;

            foreach (var session in ctx.Sessions) {
                var attendance = Attendance.CreateDefault(
                    enrollment.StudentId, session.ClassSessionId, enrollment.CourseId);

                if (session.SessionDate < today && !session.IsCancelled) {
                    var status = pastSessionIndex % 4 == 3
                        ? AttendanceStatus.Absent
                        : AttendanceStatus.Present;
                    _ = attendance.Mark(status);
                    pastSessionIndex++;
                }

                attendances.Add(attendance);
            }

            _attendanceRepository.AddRange(attendances);
            attendanceCount += attendances.Count;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SeedDemoEnrollmentDataOutputDto(enrollmentCount, attendanceCount));
    }
}