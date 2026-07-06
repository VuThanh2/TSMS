import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useGradeHub } from '@/shared/hooks/useGradeHub';
import { getPersonalSummaryApi } from './personal-summary.api';

export function usePersonalSummary() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['personal-summary'],
    queryFn: getPersonalSummaryApi,
    select: (res) => res.data,
  });

  // UC-33: kết nối SignalR, tự động làm mới khi điểm được cập nhật
  useGradeHub(() => {
    void queryClient.invalidateQueries({ queryKey: ['personal-summary'] });
  });

  return {
    summary: data,
    isLoading,
  };
}
