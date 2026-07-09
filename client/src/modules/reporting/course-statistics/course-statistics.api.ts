import api from '@/shared/lib/axios';
import type { CourseStatisticsItem } from '@/modules/reporting/shared/reporting.types';

interface CourseStatisticsResponse {
  totalCount: number;
  items: CourseStatisticsItem[];
}

export function getCourseStatisticsApi() {
  return api.get<CourseStatisticsResponse>('/reports/course-statistics');
}
