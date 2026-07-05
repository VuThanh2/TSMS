import api from '@/shared/lib/axios';
import type { StudentGradeItem, AttendanceReportItem, ScoreDistributionItem } from '@/modules/reporting/shared/reporting.types';

interface StudentGradesResponse {
  courseId: string;
  courseName: string;
  items: StudentGradeItem[];
}

interface AttendanceReportResponse {
  courseId: string;
  courseName: string;
  items: AttendanceReportItem[];
}

interface ScoreDistributionResponse {
  courseId: string;
  courseName: string;
  gradedStudentCount: number;
  items: ScoreDistributionItem[];
}

export function getStudentGradesApi(courseId: string) {
  return api.get<StudentGradesResponse>(`/reports/student-grades/${courseId}`);
}

export function getAttendanceReportApi(courseId: string) {
  return api.get<AttendanceReportResponse>(`/reports/attendance/${courseId}`);
}

export function getScoreDistributionApi(courseId: string) {
  return api.get<ScoreDistributionResponse>(`/reports/score-distribution/${courseId}`);
}
