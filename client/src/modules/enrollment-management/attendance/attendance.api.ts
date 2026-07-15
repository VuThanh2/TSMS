import api from '@/shared/lib/axios';
import type { AttendanceRecord, AttendanceStatus, SessionAttendanceSummary } from './attendance.types';

// Không truyền courseId: BE tự suy Course sở hữu ca học từ sessionId rồi mới check quyền
// Lecturer — truyền courseId rời sẽ mở đường đọc trộm điểm danh của Course khác.
export function getSessionAttendancesApi(sessionId: string) {
  return api.get<AttendanceRecord[]>(`/sessions/attendances/${sessionId}`);
}

// Chỉ trả về buổi ĐÃ CÓ Attendance record — buổi chưa ai enroll sẽ vắng mặt trong list.
export function getCourseAttendanceSummaryApi(courseId: string) {
  return api.get<SessionAttendanceSummary[]>(`/courses/attendance-summary/${courseId}`);
}

export function updateAttendanceApi(attendanceId: string, attendanceStatus: AttendanceStatus) {
  return api.put(`/attendances/${attendanceId}`, { attendanceStatus });
}
