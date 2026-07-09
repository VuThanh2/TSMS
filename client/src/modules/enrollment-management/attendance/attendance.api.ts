import api from '@/shared/lib/axios';
import type { AttendanceRecord, AttendanceStatus } from './attendance.types';

export function getSessionAttendancesApi(courseId: string, sessionId: string) {
  return api.get<AttendanceRecord[]>(`/courses/${courseId}/sessions/${sessionId}/attendances`);
}

export function updateAttendanceApi(attendanceId: string, attendanceStatus: AttendanceStatus) {
  return api.put(`/attendances/${attendanceId}`, { attendanceStatus });
}
