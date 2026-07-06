import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getAvailableCoursesApi, enrollCourseApi } from './enrollment.api';

const ENROLL_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseIsFull': 'Khóa học đã đầy chỗ.',
  'Enrollment.InvalidSessionCount': 'Phải chọn đúng 2 slot học.',
  'Enrollment.DuplicateSession': '2 slot đã chọn phải khác nhau.',
  'Enrollment.SessionNotInCourse': 'Slot không thuộc khóa học này.',
  'Enrollment.ScheduleConflict': 'Trùng lịch với khóa học bạn đã đăng ký.',
  'Validation.Failed': 'Dữ liệu không hợp lệ.',
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
      void message.success('Đăng ký khóa học thành công!');
      void queryClient.invalidateQueries({ queryKey: ['available-courses'] });
      void queryClient.invalidateQueries({ queryKey: ['my-course-enrollments'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        ENROLL_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Đăng ký thất bại.';
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
