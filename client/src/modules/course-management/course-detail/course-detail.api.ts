import api from '@/shared/lib/axios';
import type { CourseDetail, WeeklySlot } from '@/modules/course-management/shared/course.types';

export function getCourseByIdApi(courseId: string) {
  return api.get<CourseDetail>(`/courses/${courseId}`);
}

export function getWeeklySlotsApi(courseId: string) {
  return api.get<WeeklySlot[]>(`/courses/weekly-slots/${courseId}`);
}

export function updateCourseApi(courseId: string, data: { name: string; description?: string; endDate: string; maxCapacity: number }) {
  return api.put(`/courses/${courseId}`, data);
}

// Body PHẢI là `newLecturerId` — khớp ReplaceLecturerInputDto(Guid NewLecturerId) bên BE.
// Gửi `lecturerId` thì field không bind, về Guid.Empty và validator trả "NewLecturerId is required".
export function replaceLecturerApi(courseId: string, data: { newLecturerId: string }) {
  return api.put(`/courses/lecturer/${courseId}`, data);
}

// Mở cổng cho Student đăng ký. Một chiều — chưa có API đóng lại.
export function openCourseEnrollmentApi(courseId: string) {
  return api.put<{ isOpenForEnrollment: boolean }>(`/courses/open-enrollment/${courseId}`);
}

export function addWeeklySlotApi(courseId: string, data: { dayOfWeek: string; sessionType: string }) {
  return api.post<{ weeklySlotId: string; generatedSessionCount: number }>(`/courses/weekly-slots/${courseId}`, data);
}

export function removeWeeklySlotApi(courseId: string, weeklySlotId: string) {
  return api.delete(`/courses/weekly-slots/${courseId}/${weeklySlotId}`);
}

export function deleteCourseApi(courseId: string) {
  return api.delete(`/courses/${courseId}`);
}

// Soft-cancel 1 buổi học cụ thể (IsCancelled = true) — verb DELETE khớp REST controller,
// không xóa vật lý. Muốn hủy cả khung giờ lặp lại thì dùng removeWeeklySlotApi.
export function cancelClassSessionApi(courseId: string, sessionId: string) {
  return api.delete(`/courses/sessions/${courseId}/${sessionId}`);
}
