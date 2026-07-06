import type { CourseStatus } from '@/modules/course-management/shared/course.types';

export interface PersonalSummaryItem {
  courseId: string;
  courseName: string;
  status: CourseStatus;
  grade: number | null;
  totalSessions: number;
  presentCount: number;
  excusedCount: number;
  absentCount: number;
  attendanceRate: number;
}

export interface PersonalSummaryResponse {
  overallGpa: number | null;
  items: PersonalSummaryItem[];
}

export interface CourseStatisticsItem {
  courseId: string;
  courseName: string;
  lecturerName: string;
  startDate: string;
  endDate: string;
  status: CourseStatus;
  enrolledCount: number;
  averageScore: number | null;
  gradedStudentCount: number;
  ungradedStudentCount: number;
}

export interface StudentGradeItem {
  enrollmentId: string;
  studentId: string;
  studentFullName: string;
  studentEmail: string;
  grade: number | null;
}

export interface AttendanceReportItem {
  enrollmentId: string;
  studentId: string;
  studentFullName: string;
  studentEmail: string;
  totalSessions: number;
  presentCount: number;
  excusedCount: number;
  absentCount: number;
  attendanceRate: number;
}

export interface ScoreDistributionItem {
  scoreGroup: string;
  rangeStart: number;
  rangeEnd: number;
  studentCount: number;
  percentage: number;
}
