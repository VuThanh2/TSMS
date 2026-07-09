import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { getSessionAttendancesApi, updateAttendanceApi } from './attendance.api';
import type { AttendanceStatus } from './attendance.types';

const ATTENDANCE_ERROR_MESSAGES: Record<string, string> = {
  'Enrollment.SessionCancelled': 'This session has been cancelled; attendance cannot be updated.',
  'Enrollment.AttendanceNotFound': 'Attendance record not found.',
  'Enrollment.NotCourseOwner': 'You are not the lecturer in charge of this session.',
  'Validation.Failed': 'Invalid attendance data.',
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
        'Failed to update attendance.';
      void message.error(msg);
    },
  });

  return {
    attendances: attendances.data ?? [],
    isLoading: attendances.isLoading,
    updateAttendance,
  };
}
