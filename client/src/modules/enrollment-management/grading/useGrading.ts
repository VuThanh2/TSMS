import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { getCourseEnrollmentsApi, updateGradeApi } from './grading.api';

const GRADE_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseNotGradeable': 'Course is not open for grading',
  'Enrollment.NotCourseOwner': 'You don’t teach this course',
  'Enrollment.NotFound': 'Enrollment not found',
  // Giữ khoảng giá trị: đây là ràng buộc user phải biết mới nhập lại đúng được.
  'Validation.Failed': 'Grade must be between 0 and 10',
};

export function useGrading(courseId: string) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Debounce: chỉ gọi API sau khi ngừng gõ, tránh 1 request mỗi keystroke gây lag.
  const debouncedKeyword = useDebouncedValue(keyword);

  const enrollments = useQuery({
    queryKey: ['enrollments', courseId, { keyword: debouncedKeyword, page, pageSize }],
    queryFn: () =>
      getCourseEnrollmentsApi(courseId, {
        keyword: debouncedKeyword || undefined,
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
      // Không nhắc lại chuyện gửi email: GradingPanel đã ghi ngay trên bảng rồi.
      void message.success('Grade saved');
      void queryClient.invalidateQueries({ queryKey: ['enrollments', courseId] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        GRADE_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Could not save grade';
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
