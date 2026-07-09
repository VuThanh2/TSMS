import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getAvailableCoursesApi, enrollCourseApi } from './enrollment.api';

const ENROLL_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseIsFull': 'This course is full.',
  'Enrollment.InvalidSessionCount': 'You must select exactly 2 slots.',
  'Enrollment.DuplicateSession': 'The 2 selected slots must be different.',
  'Enrollment.SessionNotInCourse': 'This slot does not belong to this course.',
  'Enrollment.ScheduleConflict': 'This conflicts with the schedule of a course you are already enrolled in.',
  'Validation.Failed': 'Invalid data.',
};

export function useAvailableCourses() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const coursesQuery = useQuery({
    queryKey: ['available-courses', { page, pageSize }],
    queryFn: () => getAvailableCoursesApi({ page, pageSize }),
    select: (res) => res.data,
  });

  const enrollMutation = useMutation({
    mutationFn: enrollCourseApi,
    onSuccess: () => {
      void message.success('Enrolled in the course successfully!');
      void queryClient.invalidateQueries({ queryKey: ['available-courses'] });
      void queryClient.invalidateQueries({ queryKey: ['my-course-enrollments'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        ENROLL_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Enrollment failed.';
      void message.error(msg);
    },
  });

  return {
    courses: coursesQuery.data?.items ?? [],
    totalCount: coursesQuery.data?.totalCount ?? 0,
    isLoading: coursesQuery.isLoading,
    page,
    pageSize,
    setPage,
    setPageSize,
    enrollMutation,
  };
}
