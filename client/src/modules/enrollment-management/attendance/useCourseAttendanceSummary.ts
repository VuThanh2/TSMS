import { useQuery } from '@tanstack/react-query';

import { getCourseAttendanceSummaryApi } from './attendance.api';
import type { SessionAttendanceSummary } from './attendance.types';

// Hằng số để lúc chưa có data không sinh Map mới mỗi render (giữ tham chiếu ổn định).
const EMPTY_SUMMARY = new Map<string, SessionAttendanceSummary>();

// Số liệu điểm danh từng buổi của 1 Course, trả về Map để lưới lịch tra theo classSessionId.
// Endpoint là Lecturer-only, nhưng cả 2 grid dùng hook này đều chỉ mount ở nhánh Lecturer
// của CourseDetailPage (Admin đi nhánh canManage) — nên không cần chặn theo role ở đây.
export function useCourseAttendanceSummary(courseId: string) {
  const query = useQuery({
    queryKey: ['attendance-summary', courseId],
    queryFn: () => getCourseAttendanceSummaryApi(courseId),
    select: (res) =>
      new Map<string, SessionAttendanceSummary>(
        res.data.map((s) => [s.classSessionId, s]),
      ),
    enabled: !!courseId,
  });

  return query.data ?? EMPTY_SUMMARY;
}
