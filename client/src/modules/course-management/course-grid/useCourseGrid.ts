import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { getCoursesApi } from './course-grid.api';

export function useCourseGrid() {
  const [keyword, setKeyword] = useState('');
  const [status, setStatus] = useState<string>('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const { data, isLoading } = useQuery({
    queryKey: ['courses', { keyword, status, page, pageSize }],
    queryFn: () =>
      getCoursesApi({
        keyword: keyword || undefined,
        status: status || undefined,
        page,
        pageSize,
      }),
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
