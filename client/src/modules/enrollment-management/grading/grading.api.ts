import api from '@/shared/lib/axios';
import type { PagedResult } from '@/shared/types/api.types';
import type { EnrollmentItem } from './grading.types';

export interface GradingParams {
  keyword?: string;
  page: number;
  pageSize: number;
}

export function getCourseEnrollmentsApi(courseId: string, params: GradingParams) {
  return api.get<PagedResult<EnrollmentItem>>(`/courses/enrollments/${courseId}`, { params });
}

export function updateGradeApi(enrollmentId: string, grade: number) {
  return api.put(`/enrollments/grade/${enrollmentId}`, { grade });
}
