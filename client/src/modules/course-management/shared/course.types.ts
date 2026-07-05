export type CourseStatus = 'Upcoming' | 'Active' | 'Completed';

export interface CourseListItem {
  courseId: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  status: CourseStatus;
  maxCapacity: number;
  enrolledCount: number;
  lecturerId: string;
  lecturerName?: string;
  createdAt: string;
}

export interface CourseDetail extends CourseListItem {
  classSessions: ClassSession[];
}

export interface ClassSession {
  classSessionId: string;
  weeklySlotId: string;
  sessionDate: string;
  dayOfWeek: string;
  sessionType: string;
  isPast: boolean;
  isCancelled: boolean;
}

export interface WeeklySlot {
  weeklySlotId: string;
  courseId: string;
  dayOfWeek: string;
  sessionType: string;
}

export interface LecturerOption {
  userId: string;
  fullName: string;
  email: string;
}
