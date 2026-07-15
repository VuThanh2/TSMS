import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { getCoursesApi, getMyCourseApi } from './course-grid.api';
import type { CourseGridParams } from './course-grid.api';

// Lưới Course có search + lọc status + phân trang, dùng chung cho 2 màn hình:
//   'all'  → Admin xem mọi Course            (GET /courses)
//   'mine' → Lecturer xem Course phụ trách   (GET /courses/my-courses)
//
// Hai scope giữ queryKey root RIÊNG ('courses' vs 'my-courses') — cố tình không gộp:
// useCourseDetail + useCreateCourse đang gọi invalidateQueries(['courses']), mà TanStack
// khớp key theo prefix, nên gộp root sẽ kéo cả lưới Lecturer vào các invalidation đó.
export function useCourseGrid(scope: 'all' | 'mine' = 'all') {
  const [keyword, setKeyword] = useState('');
  const [status, setStatus] = useState<string>('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Debounce: chỉ gọi API sau khi ngừng gõ, tránh 1 request mỗi keystroke gây lag.
  // Ô input vẫn bind `keyword` để gõ mượt; chỉ giá trị vào queryKey mới bị trễ.
  const debouncedKeyword = useDebouncedValue(keyword);

  const params: CourseGridParams = {
    keyword: debouncedKeyword || undefined,
    status: status || undefined,
    page,
    pageSize,
  };

  const { data, isLoading } = useQuery({
    queryKey: [scope === 'all' ? 'courses' : 'my-courses', params],
    queryFn: () => (scope === 'all' ? getCoursesApi(params) : getMyCourseApi(params)),
    select: (res) => res.data,
  });

  return {
    courses: data?.items ?? [],
    totalCount: data?.totalCount ?? 0,
    isLoading,
    keyword,
    setKeyword,
    status,
    setStatus,
    page,
    setPage,
    pageSize,
    setPageSize,
  };
}
