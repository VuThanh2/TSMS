import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getAvailableCoursesApi, enrollCourseApi } from './enrollment.api';

const ENROLL_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseIsFull': 'Course is full',
  // Giữ dạng hành động — user tự sửa được ngay trên form chọn ca.
  'Enrollment.InvalidSessionCount': 'Select exactly 2 slots',
  'Enrollment.DuplicateSession': 'Pick 2 different slots',
  'Enrollment.SessionNotInCourse': 'Slot not available',
  'Enrollment.CourseNotOpenForEnrollment': 'Course is not open for enrollment',
  'Enrollment.ScheduleConflict': 'Clashes with your schedule',
  'Validation.Failed': 'Invalid data',
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
      void message.success('Enrolled');
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
