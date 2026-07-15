import { useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { createCourseApi, type CreateCourseRequest } from './create-course.api';

// Mã phải khớp Error.Create(...) bên backend. Trước đây file này map 'Lecturer.ScheduleConflict'
// — mã KHÔNG tồn tại ở backend, nên message thân thiện chưa từng hiện lần nào.
// Tạo Course không còn check trùng lịch dạy (lúc đó chưa có ca nào để so) — check nằm ở
// AddWeeklySlot, xem useCourseDetail.ts.
const ERROR_MESSAGES: Record<string, string> = {
  'Course.LecturerNotFound': 'Lecturer unavailable',
};

export function useCreateCourse(onSuccess?: () => void) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCourseRequest) => createCourseApi(data),
    onSuccess: () => {
      void message.success('Course created');
      void queryClient.invalidateQueries({ queryKey: ['courses'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg = ERROR_MESSAGES[code] ?? error.response?.data?.message ?? 'Something went wrong';
      void message.error(msg);
    },
  });
}
