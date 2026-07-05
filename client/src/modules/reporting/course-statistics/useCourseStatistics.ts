import { useQuery } from '@tanstack/react-query';

import { getCourseStatisticsApi } from './course-statistics.api';

export function useCourseStatistics() {
  return useQuery({
    queryKey: ['course-statistics'],
    queryFn: getCourseStatisticsApi,
    select: (res) => res.data,
  });
}
