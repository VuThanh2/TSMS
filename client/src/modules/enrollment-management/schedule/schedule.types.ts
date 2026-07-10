export type SessionType = 'Morning' | 'Afternoon';

export type StudentAttendanceStatus = 'Present' | 'Absent' | 'Excused' | null;

export interface LecturerScheduleSession {
  courseId: string;
  courseName: string;
  classSessionId: string;
  sessionDate: string; // "YYYY-MM-DD"
  dayOfWeek: string;
  sessionType: SessionType;
  isCancelled: boolean;
}

export interface StudentScheduleSession {
  courseId: string;
  courseName: string;
  classSessionId: string;
  sessionDate: string; // "YYYY-MM-DD"
  dayOfWeek: string;
  sessionType: SessionType;
  isCancelled: boolean;
  attendanceStatus: StudentAttendanceStatus;
}
