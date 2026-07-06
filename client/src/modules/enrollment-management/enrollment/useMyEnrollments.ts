import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getMyCourseEnrollmentsApi, adjustSessionApi } from './enrollment.api';

const ADJUST_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseAlreadyCompleted': 'Khóa học đã kết thúc, không thể điều chỉnh.',
  'Enrollment.AdjustSessionTypeDuplicate': 'Slot mới trùng ca (Sáng/Chiều) với slot còn lại.',
  'Enrollment.ScheduleConflict': 'Slot mới trùng lịch với khóa học khác của bạn.',
  'Enrollment.NotFound': 'Không tìm thấy đăng ký.',
  'Validation.Failed': 'Dữ liệu không hợp lệ.',
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
      void message.success('Điều chỉnh ca học thành công!');
      void queryClient.invalidateQueries({ queryKey: ['my-course-enrollments'] });
      void queryClient.invalidateQueries({ queryKey: ['student-schedule'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        ADJUST_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Điều chỉnh thất bại.';
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
