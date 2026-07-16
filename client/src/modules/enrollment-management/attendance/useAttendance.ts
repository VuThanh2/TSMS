import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getSessionAttendancesApi, updateAttendanceApi } from './attendance.api';
import type { AttendanceStatus } from './attendance.types';

const ATTENDANCE_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.SessionCancelled': 'Session was cancelled',
  'Enrollment.AttendanceNotFound': 'Record not found',
  'Enrollment.NotCourseOwner': 'You don’t teach this course',
  'Validation.Failed': 'Invalid attendance data',
};

export function useAttendance(courseId: string, sessionId: string) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const attendances = useQuery({
    queryKey: ['attendances', courseId, sessionId],
    queryFn: () => getSessionAttendancesApi(sessionId),
    select: (res) => res.data,
    enabled: !!courseId && !!sessionId,
  });

  // set để track row nào đang loading khi cập nhật từng dòng riêng lẻ
  const updateAttendance = useMutation({
    mutationFn: ({ attendanceId, status }: { attendanceId: string; status: AttendanceStatus }) =>
      updateAttendanceApi(attendanceId, status),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['attendances', courseId, sessionId] });
      // Số có mặt hiển thị trên lưới lịch phải đổi ngay theo, nếu không Lecturer chấm xong
      // vẫn thấy số cũ ngay trên cùng màn hình.
      void queryClient.invalidateQueries({ queryKey: ['attendance-summary', courseId] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const msg =
        ATTENDANCE_ERROR_MESSAGES[code] ??
        error.response?.data?.message ??
        'Could not save attendance';
      void message.error(msg);
    },
  });

  return {
    attendances: attendances.data ?? [],
    isLoading: attendances.isLoading,
    updateAttendance,
  };
}
