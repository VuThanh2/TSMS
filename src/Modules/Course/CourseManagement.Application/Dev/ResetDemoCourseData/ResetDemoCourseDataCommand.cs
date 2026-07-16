using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Entities;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using CourseManagement.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.Dev.ResetDemoCourseData;

// Không có input — luôn seed 1 bộ dữ liệu cố định (3 Completed, 3 Active, 2 Upcoming) tính theo
// DateTime.UtcNow tại thời điểm gọi, round-robin đều qua tất cả Lecturer đang Active.
public sealed record ResetDemoCourseDataCommand : IRequest<Result<ResetDemoCourseDataOutputDto>>;

public sealed class ResetDemoCourseDataCommandHandler
    : IRequestHandler<ResetDemoCourseDataCommand, Result<ResetDemoCourseDataOutputDto>> {
    private static readonly string[] SubjectNames = [
        "Lập trình Hướng đối tượng",
        "Cấu trúc Dữ liệu và Giải thuật",
        "Cơ sở dữ liệu",
        "Phát triển Ứng dụng Web",
        "Mạng máy tính",
        "Trí tuệ Nhân tạo",
        "Hệ điều hành",
        "Kỹ thuật Phần mềm",
    ];

    private readonly ICourseDataResetter _courseDataResetter;
    private readonly ICourseRepository _courseRepository;
    private readonly ILecturerLookupService _lecturerLookupService;
    private readonly ICourseUnitOfWork _unitOfWork;

    public ResetDemoCourseDataCommandHandler(
        ICourseDataResetter courseDataResetter,
        ICourseRepository courseRepository,
        ILecturerLookupService lecturerLookupService,
        ICourseUnitOfWork unitOfWork) {
        _courseDataResetter = courseDataResetter;
        _courseRepository = courseRepository;
        _lecturerLookupService = lecturerLookupService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ResetDemoCourseDataOutputDto>> Handle(
        ResetDemoCourseDataCommand request,
        CancellationToken cancellationToken) {
        // Bước 1: xóa sạch Course/WeeklySlot/ClassSession cũ.
        await _courseDataResetter.ClearAllAsync(cancellationToken);

        // Bước 2: cần Lecturer Active thật đã tồn tại. Seed không tự tạo Lecturer.
        var lecturerIds = await _lecturerLookupService.GetActiveLecturerIdsAsync(cancellationToken);
        if (lecturerIds.Count == 0)
            return Result.Failure<ResetDemoCourseDataOutputDto>(CourseErrors.NoActiveLecturerForSeeding);

        // Cache tên Lecturer 1 lần — tránh gọi GetFullNameAsync lặp lại cho mỗi Course dùng
        // chung 1 Lecturer khi round-robin.
        var lecturerNames = new Dictionary<Guid, string>();
        foreach (var id in lecturerIds)
            lecturerNames[id] = await _lecturerLookupService.GetFullNameAsync(id, cancellationToken)
                                 ?? string.Empty;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Bước 3: seed 8 Course mẫu (3 Completed, 3 Active, 2 Upcoming), set status TRỰC TIẾP
        // (không chờ Hangfire job)
        //
        // WeeklySlot: 6 Course Completed/Active (index 0-5) trải ĐỀU cả tuần — mỗi Course có
        // ca Sáng ở 1 ngày và ca Chiều ở 1 ngày KHÁC. Điểm mấu chốt để phân bổ ĐỀU: 6 ngày Sáng
        // của 6 Course PHÂN BIỆT nhau (Mon→Sat) VÀ 6 ngày Chiều cũng PHÂN BIỆT nhau (Thu,Fri,Sat,
        // Tue,Wed,Mon). Nhờ 2 tập ngày đều phân biệt:
        //   - Mỗi (Ngày, Ca) chỉ xuất hiện ở đúng 1 Course ⇒ BẤT KỲ tổ hợp Course nào cũng KHÔNG
        //     BAO GIỜ đụng ca (đúng ScheduleConflictChecker) — không cần check thêm ở seed.
        //   - Mỗi ngày trong tuần gánh đúng 1 ca Sáng + 1 ca Chiều ⇒ tải theo ngày cân bằng tuyệt đối.
        var specs = new[] {
            (Start: today.AddDays(-90), End: today.AddDays(-30), Status: CourseStatus.Completed,
                DayA: DayOfWeek.Monday, SessA: SessionType.Morning, DayB: DayOfWeek.Thursday, SessB: SessionType.Afternoon),
            (Start: today.AddDays(-75), End: today.AddDays(-15), Status: CourseStatus.Completed,
                DayA: DayOfWeek.Tuesday, SessA: SessionType.Morning, DayB: DayOfWeek.Friday, SessB: SessionType.Afternoon),
            (Start: today.AddDays(-60), End: today.AddDays(-5), Status: CourseStatus.Completed,
                DayA: DayOfWeek.Wednesday, SessA: SessionType.Morning, DayB: DayOfWeek.Saturday, SessB: SessionType.Afternoon),
            (Start: today.AddDays(-30), End: today.AddDays(30), Status: CourseStatus.Active,
                DayA: DayOfWeek.Thursday, SessA: SessionType.Morning, DayB: DayOfWeek.Tuesday, SessB: SessionType.Afternoon),
            (Start: today.AddDays(-20), End: today.AddDays(40), Status: CourseStatus.Active,
                DayA: DayOfWeek.Friday, SessA: SessionType.Morning, DayB: DayOfWeek.Wednesday, SessB: SessionType.Afternoon),
            (Start: today.AddDays(-10), End: today.AddDays(50), Status: CourseStatus.Active,
                DayA: DayOfWeek.Saturday, SessA: SessionType.Morning, DayB: DayOfWeek.Monday, SessB: SessionType.Afternoon),
            (Start: today.AddDays(7), End: today.AddDays(67), Status: CourseStatus.Upcoming,
                DayA: DayOfWeek.Tuesday, SessA: SessionType.Morning, DayB: DayOfWeek.Friday, SessB: SessionType.Afternoon), // trùng cặp slot Course #1 (demo trùng lịch, tránh Mon/Thu)
            (Start: today.AddDays(35), End: today.AddDays(95), Status: CourseStatus.Upcoming,
                DayA: DayOfWeek.Wednesday, SessA: SessionType.Morning, DayB: DayOfWeek.Saturday, SessB: SessionType.Afternoon), // trùng cặp slot Course #2 (demo trùng lịch, tránh Mon/Thu)
        };

        var activeCourseIds = new List<Guid>();
        var completedCourseIds = new List<Guid>();
        var enrollableCourses = new List<DemoSeededCourse>();

        for (var i = 0; i < specs.Length; i++) {
            var spec = specs[i];

            // Round-robin đều qua tất cả Lecturer Active — tránh dồn hết Course vào 1 người,
            // để màn "My Courses" của mỗi Lecturer đều có dữ liệu khi demo.
            var lecturerId = lecturerIds[i % lecturerIds.Count];
            var lecturerName = lecturerNames[lecturerId];
            var subjectName = SubjectNames[i % SubjectNames.Length];

            var courseNameResult = CourseName.Create(subjectName);
            if (courseNameResult.IsFailure)
                return Result.Failure<ResetDemoCourseDataOutputDto>(courseNameResult.Error);

            var dateRangeResult = DateRange.CreateForSeeding(spec.Start, spec.End);
            if (dateRangeResult.IsFailure)
                return Result.Failure<ResetDemoCourseDataOutputDto>(dateRangeResult.Error);

            var createResult = Course.Create(
                lecturerId: lecturerId,
                courseName: courseNameResult.Value,
                description: "Dữ liệu demo — được tạo tự động bởi Demo Data Reset.",
                dateRange: dateRangeResult.Value,
                maxCapacity: 8,
                lecturerName: lecturerName);

            if (createResult.IsFailure)
                return Result.Failure<ResetDemoCourseDataOutputDto>(createResult.Error);

            var course = createResult.Value;

            // Cặp WeeklySlot "chuẩn" (1 Sáng + 1 Chiều) theo spec — đây là cặp Demo Seeder sẽ enroll
            // Student vào (giữ phân bổ đều + không trùng lịch). Luôn thành công vì course còn Upcoming,
            // chưa có slot trùng ⇒ .Value an toàn.
            var morningSlot = course.AddWeeklySlot(spec.DayA, spec.SessA).Value;
            var afternoonSlot = course.AddWeeklySlot(spec.DayB, spec.SessB).Value;

            // Slot PHỤ: thêm để Student có nhiều lựa chọn khi enroll/adjust (chọn ca Sáng/Chiều khác
            // ngày). KHÔNG được seed enroll vào — chỉ là option.
            AddExtraSlots(course, spec.DayA, spec.SessA, ExtraSlotsPerSession);
            AddExtraSlots(course, spec.DayB, spec.SessB, ExtraSlotsPerSession);

            // Mở cổng đăng ký cho TẤT CẢ course demo. Bắt buộc phải làm ở đây:
            //   - Course.Create() sinh ra IsOpenForEnrollment = false, mà Student chỉ thấy course
            //     đã mở ⇒ không mở thì 2 course Upcoming biến mất khỏi Available Courses và demo
            //     "Student đăng ký khóa học" hỏng, KHÔNG có lỗi nào báo.
            //   - Phải gọi TRƯỚC TransitionStatus: OpenEnrollment() chỉ chấp nhận Upcoming.
            // Course Active/Completed cũng mở cho đúng thực tế — chúng đã qua kỳ đăng ký rồi.
            _ = course.OpenEnrollment();

            // Chuyển status qua đúng state machine sẵn có của aggregate (Upcoming→Active→Completed) —
            // không bypass invariant, chỉ gọi sớm hơn bình thường (bình thường job nền gọi).
            if (spec.Status is CourseStatus.Active or CourseStatus.Completed)
                _ = course.TransitionStatus(CourseStatus.Active);
            if (spec.Status is CourseStatus.Completed)
                _ = course.TransitionStatus(CourseStatus.Completed);

            _courseRepository.Add(course);

            // Ghi nhớ Id theo nhóm status, THEO ĐÚNG THỨ TỰ specs (index 0-2 = Completed,
            // index 3-5 = Active) — SeedDemoEnrollmentDataCommand dựa vào đúng thứ tự này để map
            // bảng phân bổ Student-Course (StudentCoursePlan), không được đảo lộn. EnrollableCourses
            // gom cả CourseId lẫn cặp slot chuẩn để Seeder enroll đúng chỗ.
            if (spec.Status == CourseStatus.Active) {
                activeCourseIds.Add(course.Id);
                enrollableCourses.Add(new DemoSeededCourse(course.Id, false, morningSlot.Id, afternoonSlot.Id));
            } else if (spec.Status == CourseStatus.Completed) {
                completedCourseIds.Add(course.Id);
                enrollableCourses.Add(new DemoSeededCourse(course.Id, true, morningSlot.Id, afternoonSlot.Id));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ResetDemoCourseDataOutputDto(
            CreatedCourseCount: specs.Length,
            LecturerCount: lecturerIds.Count,
            ActiveCourseIds: activeCourseIds,
            CompletedCourseIds: completedCourseIds,
            EnrollableCourses: enrollableCourses));
    }

    // Số slot PHỤ thêm cho mỗi ca (Sáng/Chiều) ngoài slot chuẩn — để Student có lựa chọn khi
    // enroll/adjust. 2 ⇒ mỗi Course có 3 ca Sáng + 3 ca Chiều = 6 WeeklySlot.
    private const int ExtraSlotsPerSession = 2;

    // Thêm tối đa `count` WeeklySlot cùng SessionType trên các ngày KHÁC primaryDay (rải Thứ 2 →
    // Thứ 7, bỏ Chủ nhật).
    private static void AddExtraSlots(Course course, DayOfWeek primaryDay, SessionType session, int count) {
        var added = 0;
        for (var step = 1; step <= 6 && added < count; step++) {
            var day = (DayOfWeek)(((int)primaryDay + step) % 7);
            if (day == DayOfWeek.Sunday)
                continue; // Chỉ dùng Thứ 2 → Thứ 7.
            if (course.AddWeeklySlot(day, session).IsSuccess)
                added++;
        }
    }
}