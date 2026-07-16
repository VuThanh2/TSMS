import api from '@/shared/lib/axios';
import type { PagedResult } from '@/shared/types/api.types';
import type { SortDirection } from '@/shared/hooks/useTableSort';
import type { CourseListItem } from '@/modules/course-management/shared/course.types';

export interface CourseGridParams {
  keyword?: string;
  status?: string;
  page: number;
  pageSize: number;
  // BE chỉ nhận: name | startDate | endDate | status.
  sortBy?: string;
  sortDir?: SortDirection;
}

export function getCoursesApi(params: CourseGridParams) {
  return api.get<PagedResult<CourseListItem>>('/courses', { params });
}

export function getMyCourseApi(params: CourseGridParams) {
  return api.get<PagedResult<CourseListItem>>('/courses/my-courses', { params });
}
