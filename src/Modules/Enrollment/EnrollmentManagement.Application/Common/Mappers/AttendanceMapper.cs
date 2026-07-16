using EnrollmentManagement.Application.Attendances.GetCourseAttendanceSummary;
using EnrollmentManagement.Application.Attendances.GetSessionAttendances;
using EnrollmentManagement.Application.Attendances.MarkAttendance;
using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;

namespace EnrollmentManagement.Application.Common.Mappers;

public static class AttendanceMapper {
    // ── GetSessionAttendances

    public static GetSessionAttendancesOutputDto ToGetSessionAttendancesOutputDto(
        Attendance attendance,
        string? studentFullName) =>
        new(
            AttendanceId: attendance.Id,
            StudentId: attendance.StudentId,
            StudentFullName: studentFullName,
            AttendanceStatus: attendance.Status.ToString(),
            // Trả về null nếu status vẫn là Absent mặc định (chưa được Lecturer chỉnh sửa).
            MarkedAt: attendance.Status == AttendanceStatus.Absent
                ? null
                : attendance.UpdatedAt);

    // ── GetCourseAttendanceSummary

    public static GetCourseAttendanceSummaryOutputDto ToGetCourseAttendanceSummaryOutputDto(
        SessionAttendanceCount count) =>
        new(
            ClassSessionId: count.ClassSessionId,
            PresentCount: count.PresentCount,
            ExcusedCount: count.ExcusedCount,
            AbsentCount: count.AbsentCount,
            TotalCount: count.PresentCount + count.ExcusedCount + count.AbsentCount,
            // Cùng heuristic với MarkedAt ở trên: toàn bộ Absent = chưa ai chấm, vì Absent
            // là giá trị pre-populate lúc enroll.
            IsMarked: count.PresentCount > 0 || count.ExcusedCount > 0);

    // ── MarkAttendance

    public static MarkAttendanceOutputDto ToMarkAttendanceOutputDto(Attendance attendance) =>
        new(
            AttendanceId: attendance.Id,
            ClassSessionId: attendance.ClassSessionId,
            StudentId: attendance.StudentId,
            AttendanceStatus: attendance.Status.ToString());
}