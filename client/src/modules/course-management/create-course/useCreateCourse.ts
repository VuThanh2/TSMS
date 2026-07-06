import { useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { createCourseApi, type CreateCourseRequest } from './create-course.api';

const ERROR_MESSAGES: Record<string, string> = {
  'Lecturer.NotFound': 'Không tìm thấy giảng viên.',
  'Lecturer.NotActive': 'Giảng viên đã bị vô hiệu hóa.',
  'Lecturer.ScheduleConflict': 'Giảng viên bị trùng lịch dạy.',
};

export function useCreateCourse(onSuccess?: () => void) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCourseRequest) => createCourseApi(data),
    onSuccess: () => {
      void message.success('Tạo khóa học thành công!');
      void queryClient.invalidateQueries({ queryKey: ['courses'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg = ERROR_MESSAGES[code] ?? error.response?.data?.message ?? 'Đã có lỗi xảy ra.';
      void message.error(msg);
    },
  });
}
