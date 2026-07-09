import { useQuery } from '@tanstack/react-query';

import { getMyCourseApi } from '@/modules/course-management/course-grid/course-grid.api';

// Danh sách Course mà Lecturer phụ trách — dùng cho Select ở trang Grading và
// Attendance (mỗi trang tự chọn Course rồi thao tác). Lấy 1 lần, pageSize lớn để
// đủ options mà không cần phân trang trong dropdown.
export function useLecturerCourses() {
  const { data, isLoading } = useQuery({
    queryKey: ['lecturer-courses-options'],
    queryFn: () => getMyCourseApi({ page: 1, pageSize: 100 }),
    select: (res) => res.data.items ?? [],
  });

  return { courses: data ?? [], isLoading };
}
