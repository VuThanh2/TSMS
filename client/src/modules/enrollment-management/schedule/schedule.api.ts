import api from '@/shared/lib/axios';
import type { LecturerScheduleSession, StudentScheduleSession } from './schedule.types';

// Backend wraps list in { items: [...] } via GetLecturerScheduleResponse record
export function getLecturerScheduleApi() {
  return api.get<{ items: LecturerScheduleSession[] }>('/schedule/lecturer');
}

export function getStudentScheduleApi() {
  return api.get<{ items: StudentScheduleSession[] }>('/schedule/student');
}
