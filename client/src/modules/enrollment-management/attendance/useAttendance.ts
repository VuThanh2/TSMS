import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getSessionAttendancesApi, updateAttendanceApi } from './attendance.api';
import type { AttendanceStatus } from './attendance.types';

const ATTENDANCE_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.SessionCancelled': 'Buổi học đã bị hủy, không thể cập nhật điểm danh.',
  'Enrollment.AttendanceNotFound': 'Không tìm thấy bản ghi điểm danh.',
  'Enrollment.NotCourseOwner': 'Bạn không phải là giảng viên phụ trách buổi học này.',
  'Validation.Failed': 'Dữ liệu điểm danh không hợp lệ.',
};

export function useAttendance(courseId: string, sessionId: string) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const attendances = useQuery({
    queryKey: ['attendances', courseId, sessionId],
    queryFn: () => getSessionAttendancesApi(courseId, sessionId),
    select: (res) => res.data,
    enabled: !!courseId && !!sessionId,
  });

  // set để track row nào đang loading khi cập nhật từng dòng riêng lẻ
  const updateAttendance = useMutation({
    mutationFn: ({ attendanceId, status }: { attendanceId: string; status: AttendanceStatus }) =>
      updateAttendanceApi(attendanceId, status),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['attendances', courseId, sessionId] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        ATTENDANCE_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Cập nhật điểm danh thất bại.';
      void message.error(msg);
    },
  });

  return {
    attendances: attendances.data ?? [],
    isLoading: attendances.isLoading,
    updateAttendance,
  };
}
