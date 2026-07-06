import { useQuery } from '@tanstack/react-query';
import { getLecturerScheduleApi } from './schedule.api';
import type { LecturerScheduleSession } from './schedule.types';

export function useSchedule() {
  const { data, isLoading } = useQuery({
    queryKey: ['lecturer-schedule'],
    queryFn: getLecturerScheduleApi,
    select: (res) => res.data.items ?? [],
  });

  // group theo ngày để Calendar cellRender tra cứu nhanh
  const sessionsByDate = (data ?? []).reduce<Record<string, LecturerScheduleSession[]>>(
    (acc, s) => {
      (acc[s.sessionDate] ??= []).push(s);
      return acc;
    },
    {},
  );

  return {
    sessions: data ?? [],
    sessionsByDate,
    isLoading,
  };
}
