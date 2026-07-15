import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import {
  getCourseByIdApi,
  getWeeklySlotsApi,
  updateCourseApi,
  replaceLecturerApi,
  addWeeklySlotApi,
  removeWeeklySlotApi,
  deleteCourseApi,
  cancelClassSessionApi,
  openCourseEnrollmentApi,
} from './course-detail.api';

export function useCourseDetail(courseId: string) {
  const course = useQuery({
    queryKey: ['course', courseId],
    queryFn: () => getCourseByIdApi(courseId),
    select: (res) => res.data,
  });

  const weeklySlots = useQuery({
    queryKey: ['weekly-slots', courseId],
    queryFn: () => getWeeklySlotsApi(courseId),
    select: (res) => res.data,
  });

  return { course, weeklySlots };
}

function useInvalidateCourse(courseId: string) {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ['course', courseId] });
    void queryClient.invalidateQueries({ queryKey: ['weekly-slots', courseId] });
    void queryClient.invalidateQueries({ queryKey: ['courses'] });
  };
}

export function useUpdateCourse(courseId: string, onSuccess?: () => void) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { name: string; description?: string; endDate: string; maxCapacity: number }) =>
      updateCourseApi(courseId, data),
    onSuccess: () => {
      void message.success('Course updated');
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.CompletedCourseIsImmutable': 'Course has ended',
        'Course.MaxCapacityBelowEnrolledCount': 'Capacity is below the enrolled count',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useReplaceLecturer(courseId: string, onSuccess?: () => void) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { newLecturerId: string }) => replaceLecturerApi(courseId, data),
    onSuccess: () => {
      void message.success('Lecturer changed');
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      // 'Lecturer.ScheduleConflict' trước đây được map ở đây nhưng backend không hề có mã đó —
      // message chết. Mã thật là Course.LecturerSlotConflict.
      const msgs: Record<string, string> = {
        'Course.LecturerAlreadyAssigned': 'Lecturer already assigned',
        'Course.LecturerSlotConflict': 'Lecturer already teaches at one of these times',
        'Course.LecturerNotFound': 'Lecturer unavailable',
        'Course.CompletedCourseIsImmutable': 'Course has ended',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useOpenCourseEnrollment(courseId: string) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: () => openCourseEnrollmentApi(courseId),
    onSuccess: () => {
      void message.success('Enrollment opened');
      invalidate();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        // Giữ dạng hành động vì đây là lỗi user tự sửa được ngay.
        'Course.MinimumWeeklySlotsRequiredToOpen': 'Add at least 2 slots first',
        'Course.OnlyUpcomingCourseCanOpenEnrollment': 'Course has already started',
        'Course.NotFound': 'Course not found',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useDeleteCourse(courseId: string) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => deleteCourseApi(courseId),
    onSuccess: () => {
      void message.success('Course deleted');
      void queryClient.invalidateQueries({ queryKey: ['courses'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.CourseHasEnrollments': 'Students are already enrolled',
        'Course.OnlyUpcomingCourseCanBeDeleted': 'Course has already started',
        'Course.NotFound': 'Course not found',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useCancelClassSession(courseId: string) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (sessionId: string) => cancelClassSessionApi(courseId, sessionId),
    onSuccess: () => {
      void message.success('Session cancelled');
      invalidate();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.CannotModifyPastClassSession': 'Session has already passed',
        'Course.ClassSessionAlreadyCancelled': 'Session already cancelled',
        'Course.CompletedCourseIsImmutable': 'Course has ended',
        'Course.ClassSessionNotFound': 'Session not found',
        'Course.NotFound': 'Course not found',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useAddWeeklySlot(courseId: string, onSuccess?: () => void) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { dayOfWeek: string; sessionType: string }) => addWeeklySlotApi(courseId, data),
    onSuccess: (res) => {
      // Giữ số buổi: Admin không thấy con số này ở đâu khác, và nó xác nhận khoảng ngày đúng.
      void message.success(`Slot added · ${res.data.generatedSessionCount} sessions`);
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'WeeklySlot.Duplicate': 'Slot already exists',
        // Check trùng lịch dạy nằm ở đây chứ không phải lúc tạo Course — tới bước này mới biết
        // được ca cụ thể, mà thiếu ca thì không xác định được có đụng nhau thật hay không.
        'Course.LecturerSlotConflict': 'Lecturer already teaches at this time',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useRemoveWeeklySlot(courseId: string) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (weeklySlotId: string) => removeWeeklySlotApi(courseId, weeklySlotId),
    onSuccess: () => {
      void message.success('Slot removed');
      invalidate();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'WeeklySlot.InUse': 'Students are enrolled in this slot',
        'WeeklySlot.MinimumRequired': 'At least 2 slots are required',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}
