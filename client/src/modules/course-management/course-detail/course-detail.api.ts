import api from '@/shared/lib/axios';
import type { CourseDetail, WeeklySlot } from '@/modules/course-management/shared/course.types';

export function getCourseByIdApi(courseId: string) {
  return api.get<CourseDetail>(`/courses/${courseId}`);
}

export function getWeeklySlotsApi(courseId: string) {
  return api.get<WeeklySlot[]>(`/courses/${courseId}/weekly-slots`);
}

export function updateCourseApi(courseId: string, data: { name: string; description?: string; endDate: string; maxCapacity: number }) {
  return api.put(`/courses/${courseId}`, data);
}

export function replaceLecturerApi(courseId: string, data: { lecturerId: string }) {
  return api.put(`/courses/${courseId}/lecturer`, data);
}

export function addWeeklySlotApi(courseId: string, data: { dayOfWeek: string; sessionType: string }) {
  return api.post<{ weeklySlotId: string; generatedSessionCount: number }>(`/courses/${courseId}/weekly-slots`, data);
}

export function removeWeeklySlotApi(courseId: string, weeklySlotId: string) {
  return api.delete(`/courses/${courseId}/weekly-slots/${weeklySlotId}`);
}
