using CourseManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EnrollmentManagement.Infrastructure.Services;

// Implement cross-BC interface IEnrollmentAttendanceSync (định nghĩa ở Course.Application).
// Enrollment sở hữu Attendance nên back-fill được xử lý ở đây. Đọc ClassSession của Course
// qua ICourseEnrollmentService (interface cross-BC hướng ngược lại) — KHÔNG ref Course.Domain.
public class EnrollmentAttendanceSyncService : IEnrollmentAttendanceSync {
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IEnrollmentUnitOfWork _unitOfWork;
    private readonly ILogger<EnrollmentAttendanceSyncService> _logger;

    public EnrollmentAttendanceSyncService(
        IEnrollmentRepository enrollmentRepository,
        IAttendanceRepository attendanceRepository,
        ICourseEnrollmentService courseEnrollmentService,
        IEnrollmentUnitOfWork unitOfWork,
        ILogger<EnrollmentAttendanceSyncService> logger) {
        _enrollmentRepository = enrollmentRepository;
        _attendanceRepository = attendanceRepository;
        _courseEnrollmentService = courseEnrollmentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task BackfillAttendanceForCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId, cancellationToken);
        if (enrollments.Count == 0)
            return;

        var createdTotal = 0;

        foreach (var enrollment in enrollments) {
            // Các WeeklySlot student đã chọn — Attendance chỉ áp cho ClassSession thuộc các slot này
            // (khớp model "student dự đúng 2 slot đã chọn").
            var slotIds = enrollment.EnrolledSessions
                .Select(s => s.WeeklySlotId)
                .Distinct()
                .ToList();
            if (slotIds.Count == 0)
                continue;

            var sessions = await _courseEnrollmentService.GetClassSessionsByWeeklySlotIdsAsync(
                slotIds, cancellationToken);
            if (sessions.Count == 0)
                continue;

            var sessionIds = sessions.Select(s => s.ClassSessionId).ToList();
            var existing = await _attendanceRepository.GetByStudentAndSessionIdsAsync(
                enrollment.StudentId, sessionIds, cancellationToken);
            var existingSessionIds = existing.Select(a => a.ClassSessionId).ToHashSet();

            var missing = sessions
                .Where(s => !existingSessionIds.Contains(s.ClassSessionId))
                .Select(s => Attendance.CreateDefault(enrollment.StudentId, s.ClassSessionId, courseId))
                .ToList();

            if (missing.Count > 0) {
                _attendanceRepository.AddRange(missing);
                createdTotal += missing.Count;
            }
        }

        if (createdTotal > 0) {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Backfilled {Count} attendance records for CourseId {CourseId}.",
                createdTotal, courseId);
        }
    }
}
