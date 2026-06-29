using EnrollmentManagement.Application.Attendances.GetSessionAttendances;
using EnrollmentManagement.Application.Attendances.MarkAttendance;
using EnrollmentManagement.Domain.Entities;
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

    // ── MarkAttendance

    public static MarkAttendanceOutputDto ToMarkAttendanceOutputDto(Attendance attendance) =>
        new(
            AttendanceId: attendance.Id,
            ClassSessionId: attendance.ClassSessionId,
            StudentId: attendance.StudentId,
            AttendanceStatus: attendance.Status.ToString());
}