import { useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { createCourseApi, type CreateCourseRequest } from './create-course.api';

const ERROR_MESSAGES: Record<string, string> = {
  'Lecturer.NotFound': 'Lecturer not found.',
  'Lecturer.NotActive': 'This lecturer has been deactivated.',
  'Lecturer.ScheduleConflict': 'This lecturer has a teaching schedule conflict.',
};

export function useCreateCourse(onSuccess?: () => void) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCourseRequest) => createCourseApi(data),
    onSuccess: () => {
      void message.success('Course created successfully!');
      void queryClient.invalidateQueries({ queryKey: ['courses'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg = ERROR_MESSAGES[code] ?? error.response?.data?.message ?? 'Something went wrong.';
      void message.error(msg);
    },
  });
}
