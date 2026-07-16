import api from '@/shared/lib/axios';
import type { PagedResult } from '@/shared/types/api.types';
import type { AvailableCourse, MyCourseItem } from './enrollment.types';
import type { WeeklySlot } from '@/modules/course-management/shared/course.types';

export interface CourseListParams {
  page: number;
  pageSize: number;
  keyword?: string;
}

export function getAvailableCoursesApi(params: CourseListParams) {
  return api.get<PagedResult<AvailableCourse>>('/courses/available', { params });
}

export function getMyCourseEnrollmentsApi(params: CourseListParams) {
  return api.get<PagedResult<MyCourseItem>>('/enrollments/my-courses', { params });
}

export function getCourseWeeklySlotsApi(courseId: string) {
  return api.get<WeeklySlot[]>(`/courses/weekly-slots/${courseId}`);
}

export function enrollCourseApi(data: { courseId: string; weeklySlotIds: string[] }) {
  return api.post('/enrollments', data);
}

export function adjustSessionApi(
  enrollmentId: string,
  data: { oldWeeklySlotId: string; newWeeklySlotId: string },
) {
  return api.put(`/enrollments/sessions/${enrollmentId}`, data);
}
