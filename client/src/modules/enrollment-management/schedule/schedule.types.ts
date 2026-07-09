export type SessionType = 'Morning' | 'Afternoon';

export type StudentAttendanceStatus = 'Present' | 'Absent' | 'Excused' | null;

export interface LecturerScheduleSession {
  courseId: string;
  courseName: string;
  classSessionId: string;
  sessionDate: string; // "YYYY-MM-DD"
  dayOfWeek: string;
  sessionType: SessionType;
}

export interface StudentScheduleSession {
  courseId: string;
  courseName: string;
  classSessionId: string;
  sessionDate: string; // "YYYY-MM-DD"
  dayOfWeek: string;
  sessionType: SessionType;
  attendanceStatus: StudentAttendanceStatus;
}
