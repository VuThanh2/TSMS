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
      void message.success('Course updated successfully!');
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.AlreadyCompleted': 'This course has ended and can no longer be edited.',
        'Course.MaxCapacityBelowEnrolledCount': 'Maximum capacity cannot be lower than the number of enrolled students.',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong.');
    },
  });
}

export function useReplaceLecturer(courseId: string, onSuccess?: () => void) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { lecturerId: string }) => replaceLecturerApi(courseId, data),
    onSuccess: () => {
      void message.success('Lecturer changed successfully!');
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.SameLecturer': 'This lecturer is already assigned to the course.',
        'Lecturer.ScheduleConflict': 'This lecturer has a teaching schedule conflict.',
        'Course.AlreadyCompleted': 'This course has ended.',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong.');
    },
  });
}

export function useDeleteCourse(courseId: string) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => deleteCourseApi(courseId),
    onSuccess: () => {
      void message.success('Course deleted.');
      void queryClient.invalidateQueries({ queryKey: ['courses'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'Course.CourseHasEnrollments': 'Cannot delete — students are already enrolled in this course.',
        'Course.OnlyUpcomingCourseCanBeDeleted': 'Only an upcoming course (not yet started) can be deleted.',
        'Course.NotFound': 'Course not found.',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong.');
    },
  });
}

export function useAddWeeklySlot(courseId: string, onSuccess?: () => void) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (data: { dayOfWeek: string; sessionType: string }) => addWeeklySlotApi(courseId, data),
    onSuccess: (res) => {
      void message.success(`Slot added — automatically generated ${res.data.generatedSessionCount} sessions.`);
      invalidate();
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      if (code === 'WeeklySlot.Duplicate') {
        void message.error('This slot already exists.');
      } else {
        void message.error(error.response?.data?.message ?? 'Something went wrong.');
      }
    },
  });
}

export function useRemoveWeeklySlot(courseId: string) {
  const { message } = App.useApp();
  const invalidate = useInvalidateCourse(courseId);
  return useMutation({
    mutationFn: (weeklySlotId: string) => removeWeeklySlotApi(courseId, weeklySlotId),
    onSuccess: () => {
      void message.success('Slot removed.');
      invalidate();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const msgs: Record<string, string> = {
        'WeeklySlot.InUse': 'Cannot remove — students are still enrolled in this slot.',
        'WeeklySlot.MinimumRequired': 'At least 2 slots are required — cannot remove any more.',
      };
      const code = error.response?.data?.code ?? '';
      void message.error(msgs[code] ?? error.response?.data?.message ?? 'Something went wrong.');
    },
  });
}
