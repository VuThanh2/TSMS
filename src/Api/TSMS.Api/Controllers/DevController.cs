using CourseManagement.Application.Dev.ResetDemoCourseData;
using CourseManagement.Infrastructure.Services;
using EnrollmentManagement.Application.Dev.ResetDemoEnrollmentData;
using EnrollmentManagement.Application.Dev.SeedDemoEnrollmentData;
using EnrollmentManagement.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Dev.ResetDemoReportingData;
using TSMS.Api.Options;

namespace TSMS.Api.Controllers;

// Controller này CỐ TÌNH đặt ở TSMS.Api (Composition Root), KHÔNG ở Presentation layer của
// 1 BC cụ thể — vì thao tác reset đụng tới cả 3 module (Course/Enrollment/Reporting) cùng lúc.
// Cùng tiền lệ với HealthChecks (ServiceCollectionExtensions) đã tham chiếu cả 4 DbContext trực
// tiếp từ Api layer cho mục đích kỹ thuật/ops — KHÔNG phải business logic (business logic vẫn
// nằm trọn trong Application layer của từng BC, Controller chỉ gọi tuần tự qua MediatR).
[ApiController]
[Route("api/dev")]
[Authorize(Roles = "Admin")]
public class DevController : ControllerBase {
    private readonly ISender _sender;
    private readonly DemoOptions _demoOptions;
    private readonly CourseOutboxProcessor _courseOutboxProcessor;
    private readonly EnrollmentOutboxProcessor _enrollmentOutboxProcessor;

    public DevController(
        ISender sender,
        DemoOptions demoOptions,
        CourseOutboxProcessor courseOutboxProcessor,
        EnrollmentOutboxProcessor enrollmentOutboxProcessor) {
        _sender = sender;
        _demoOptions = demoOptions;
        _courseOutboxProcessor = courseOutboxProcessor;
        _enrollmentOutboxProcessor = enrollmentOutboxProcessor;
    }

    // POST /api/dev/reset-demo-data
    // Guard 2 lớp: [Authorize(Roles="Admin")] ở trên + check config flag ở đây.
    [HttpPost("reset-demo-data")]
    public async Task<IActionResult> ResetDemoData(CancellationToken cancellationToken) {
        // Trả NotFound (không phải Forbid) khi tắt — tránh tiết lộ sự tồn tại của endpoint
        // xóa dữ liệu hàng loạt này ở 1 bản deploy không phải demo.
        if (!_demoOptions.EnableReset)
            return NotFound();

        // Thứ tự bắt buộc — 4 bước, không hoán đổi được:
        // 1. Xóa Enrollment CŨ trước Course CŨ — EnrolledSession/Attendance tham chiếu
        //    WeeklySlotId/ClassSessionId của Course sắp bị xóa
        var enrollmentWipeResult = await _sender.Send(new ResetDemoEnrollmentDataCommand(), cancellationToken);
        if (enrollmentWipeResult.IsFailure)
            return BadRequest(new { enrollmentWipeResult.Error.Code, enrollmentWipeResult.Error.Message });

        // 2. Xóa + tạo lại Course MỚI — trả về CourseId theo nhóm Active/Completed cho bước 3.
        var courseResult = await _sender.Send(new ResetDemoCourseDataCommand(), cancellationToken);
        if (courseResult.IsFailure)
            return BadRequest(new { courseResult.Error.Code, courseResult.Error.Message });

        // 3. Enroll Student vào Course Active/Completed vừa tạo — PHẢI chạy sau bước 2 vì cần
        //    CourseId/WeeklySlotId MỚI. Student không tự làm được việc này qua UI (EnrollCourseCommand
        //    chỉ cho Course Upcoming) nên phải seed thẳng.
        var enrollTargets = courseResult.Value.EnrollableCourses
            .Select(c => new DemoEnrollTarget(c.CourseId, c.IsCompleted, c.MorningSlotId, c.AfternoonSlotId))
            .ToList();

        var enrollmentSeedResult = await _sender.Send(
            new SeedDemoEnrollmentDataCommand(enrollTargets),
            cancellationToken);
        if (enrollmentSeedResult.IsFailure)
            return BadRequest(new { enrollmentSeedResult.Error.Code, enrollmentSeedResult.Error.Message });

        // 4. Xóa ReadModel Reporting cũ — sẽ được build lại ngay ở bước 5 (không chờ Hangfire).
        var reportingResult = await _sender.Send(new ResetDemoReportingDataCommand(), cancellationToken);
        if (reportingResult.IsFailure)
            return BadRequest(new { reportingResult.Error.Code, reportingResult.Error.Message });

        // 5. Drain Outbox ĐỒNG BỘ, ĐÚNG THỨ TỰ Course → Enrollment — thay vì chờ 2 Hangfire job
        //    (mỗi phút, chạy độc lập) tự xử lý. Bắt buộc Course TRƯỚC Enrollment: handler
        //    StudentEnrolledEvent cần CourseStatisticsView đã tồn tại mới IncrementEnrolledCount
        //    được (nếu chưa có thì bỏ qua VĨNH VIỄN — handler idempotent, không sửa lại).
        await _courseOutboxProcessor.ExecuteAsync(cancellationToken);
        await _enrollmentOutboxProcessor.ExecuteAsync(cancellationToken);

        return Ok(new {
            Course = courseResult.Value,
            Enrollment = enrollmentSeedResult.Value,
            ReportingCleared = reportingResult.Value.Success
        });
    }
}