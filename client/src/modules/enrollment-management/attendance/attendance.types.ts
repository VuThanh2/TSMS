export type AttendanceStatus = 'Present' | 'Absent' | 'Excused';

export interface AttendanceRecord {
  attendanceId: string;
  studentId: string;
  studentFullName: string;
  attendanceStatus: AttendanceStatus;
  markedAt: string | null;
}
