import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import type { AxiosError } from 'axios';

import {
  getCourseByIdApi,
  getWeeklySlotsApi,
  updateCourseApi,
  replaceLecturerApi,
  addWeeklySlotApi,
  removeWeeklySlotApi,
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
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { name: string; description?: string; endDate: string; maxCapacity: number }) =>
      updateCourseApi(courseId, data),
    onSuccess: () => {
      void message.success('Cập nhật khóa học thành công!');
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.AlreadyCompleted': 'Khóa học đã kết thúc, không thể chỉnh sửa.',
        'Course.MaxCapacityBelowEnrolledCount': 'Sĩ số tối đa không thể nhỏ hơn số sinh viên đã đăng ký.',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Đã có lỗi xảy ra.');
    },
  });
}

export function useReplaceLecturer(courseId: string, onSuccess?: () => void) {
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { lecturerId: string }) => replaceLecturerApi(courseId, data),
    onSuccess: () => {
      void message.success('Thay đổi giảng viên thành công!');
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.SameLecturer': 'Giảng viên này đã được gán cho khóa học.',
        'Lecturer.ScheduleConflict': 'Giảng viên bị trùng lịch dạy.',
        'Course.AlreadyCompleted': 'Khóa học đã kết thúc.',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Đã có lỗi xảy ra.');
    },
  });
}

export function useAddWeeklySlot(courseId: string, onSuccess?: () => void) {
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { dayOfWeek: string; sessionType: string }) => addWeeklySlotApi(courseId, data),
    onSuccess: (res) => {
      void message.success(`Đã thêm slot — tự sinh ${res.data.generatedSessionCount} buổi học.`);
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      if (code === 'WeeklySlot.Duplicate') {
        void message.error('Slot này đã tồn tại.');
      } else {
        void message.error(error.response?.data?.message ?? 'Đã có lỗi xảy ra.');
      }
    },
  });
}

export function useRemoveWeeklySlot(courseId: string) {
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (weeklySlotId: string) => removeWeeklySlotApi(courseId, weeklySlotId),
    onSuccess: () => {
      void message.success('Đã xóa slot.');
      invalidate();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'WeeklySlot.InUse': 'Không thể xóa — còn sinh viên đang đăng ký slot này.',
        'WeeklySlot.MinimumRequired': 'Cần tối thiểu 2 slot — không thể xóa thêm.',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Đã có lỗi xảy ra.');
    },
  });
}
