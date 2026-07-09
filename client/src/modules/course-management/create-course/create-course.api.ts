import api from '@/shared/lib/axios';
import type { PagedResult } from '@/shared/types/api.types';
import type { CourseListItem, LecturerOption } from '@/modules/course-management/shared/course.types';

export interface CreateCourseRequest {
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  maxCapacity: number;
  lecturerId: string;
}

export function createCourseApi(data: CreateCourseRequest) {
  return api.post<CourseListItem>('/courses', data);
}

export function getLecturersApi(params: { search?: string; page: number; pageSize: number }) {
  return api.get<PagedResult<LecturerOption>>('/users/lecturers', { params });
}
