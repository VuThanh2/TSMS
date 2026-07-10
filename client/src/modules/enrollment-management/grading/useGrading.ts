import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { getCourseEnrollmentsApi, updateGradeApi } from './grading.api';

const GRADE_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.CourseNotGradeable': 'Grades can only be entered when the course is Active or Completed.',
  'Enrollment.NotCourseOwner': 'You are not the lecturer in charge of this course.',
  'Enrollment.NotFound': 'Enrollment not found.',
  'Validation.Failed': 'The grade must be between 0 and 10.',
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
      void message.success('Grade saved. The system will automatically email a notification to the student.');
      void queryClient.invalidateQueries({ queryKey: ['enrollments', courseId] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        GRADE_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Failed to save grade.';
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
