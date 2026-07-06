import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getCourseEnrollmentsApi, updateGradeApi } from './grading.api';

const GRADE_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseNotGradeable': 'Chỉ có thể nhập điểm khi khóa học đang Active hoặc Completed.',
  'Enrollment.NotCourseOwner': 'Bạn không phải là giảng viên phụ trách khóa học này.',
  'Enrollment.NotFound': 'Không tìm thấy đăng ký.',
  'Validation.Failed': 'Điểm phải nằm trong khoảng 0–10.',
};

export function useGrading(courseId: string) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const enrollments = useQuery({
    queryKey: ['enrollments', courseId, { keyword, page, pageSize }],
    queryFn: () =>
      getCourseEnrollmentsApi(courseId, {
        keyword: keyword || undefined,
        page,
        pageSize,
      }),
    select: (res) => res.data,
    enabled: !!courseId,
  });

  const updateGrade = useMutation({
    mutationFn: ({ enrollmentId, grade }: { enrollmentId: string; grade: number }) =>
      updateGradeApi(enrollmentId, grade),
    onSuccess: () => {
      void message.success('Điểm đã được lưu. Hệ thống sẽ tự gửi email thông báo cho sinh viên.');
      void queryClient.invalidateQueries({ queryKey: ['enrollments', courseId] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        GRADE_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Lưu điểm thất bại.';
      void message.error(msg);
    },
  });

  return {
    enrollments: enrollments.data?.items ?? [],
    totalCount: enrollments.data?.totalCount ?? 0,
    isLoading: enrollments.isLoading,
    keyword,
    setKeyword,
    page,
    setPage,
    pageSize,
    setPageSize,
    updateGrade,
  };
}
