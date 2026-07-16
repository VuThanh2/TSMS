export type AttendanceStatus = 'Present' | 'Absent' | 'Excused';

export interface AttendanceRecord {
  attendanceId: string;
  studentId: string;
  studentFullName: string;
  attendanceStatus: AttendanceStatus;
  markedAt: string | null;
}

// Số liệu điểm danh của 1 buổi, dùng để hiển thị trên lưới lịch tuần.
// isMarked = false: Lecturer chưa từng chấm buổi này (BE suy ra từ heuristic all-Absent,
// vì Absent vừa là default lúc enroll vừa là giá trị thật — xem GetCourseAttendanceSummaryDto).
export interface SessionAttendanceSummary {
  classSessionId: string;
  presentCount: number;
  excusedCount: number;
  absentCount: number;
  totalCount: number;
  isMarked: boolean;
}
