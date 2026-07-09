import type { CourseStatus } from '@/modules/course-management/shared/course.types';

export interface AvailableCourse {
  courseId: string;
  name: string;
  lecturerName: string;
  startDate: string;
  endDate: string;
  maxCapacity: number;
  enrolledCount: number;
}

export interface MyCourseItem {
  enrollmentId: string;
  courseId: string;
  courseName: string;
  status: CourseStatus;
  grade: number | null;
}
