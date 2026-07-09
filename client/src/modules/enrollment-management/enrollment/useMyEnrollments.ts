import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getMyCourseEnrollmentsApi, adjustSessionApi } from './enrollment.api';

const ADJUST_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseAlreadyCompleted': 'This course has ended and can no longer be adjusted.',
  'Enrollment.AdjustSessionTypeDuplicate': 'The new slot has the same session (Morning/Afternoon) as your remaining slot.',
  'Enrollment.ScheduleConflict': 'The new slot conflicts with the schedule of another course you are enrolled in.',
  'Enrollment.NotFound': 'Enrollment not found.',
  'Validation.Failed': 'Invalid data.',
};

export function useMyEnrollments() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const myCoursesQuery = useQuery({
    queryKey: ['my-course-enrollments', { page, pageSize }],
    queryFn: () => getMyCourseEnrollmentsApi({ page, pageSize }),
    select: (res) => res.data,
  });

  const adjustSessionMutation = useMutation({
    mutationFn: ({
      enrollmentId,
      oldWeeklySlotId,
      newWeeklySlotId,
    }: {
      enrollmentId: string;
      oldWeeklySlotId: string;
      newWeeklySlotId: string;
    }) => adjustSessionApi(enrollmentId, { oldWeeklySlotId, newWeeklySlotId }),
    onSuccess: () => {
      void message.success('Session adjusted successfully!');
      void queryClient.invalidateQueries({ queryKey: ['my-course-enrollments'] });
      void queryClient.invalidateQueries({ queryKey: ['student-schedule'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        ADJUST_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Adjustment failed.';
      void message.error(msg);
    },
  });

  return {
    myCourses: myCoursesQuery.data?.items ?? [],
    totalCount: myCoursesQuery.data?.totalCount ?? 0,
    isLoading: myCoursesQuery.isLoading,
    page,
    pageSize,
    setPage,
    setPageSize,
    adjustSessionMutation,
  };
}
