import api from '@/shared/lib/axios';
import type { PagedResult } from '@/shared/types/api.types';
import type { CourseListItem } from '@/modules/course-management/shared/course.types';

export interface CourseGridParams {
  keyword?: string;
  status?: string;
  page: number;
  pageSize: number;
}

export function getCoursesApi(params: CourseGridParams) {
  return api.get<PagedResult<CourseListItem>>('/courses', { params });
}

export function getMyCourseApi(params: CourseGridParams) {
  return api.get<PagedResult<CourseListItem>>('/courses/my-courses', { params });
}
