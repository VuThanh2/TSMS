import { useQuery } from '@tanstack/react-query';
import { getLecturerScheduleApi } from './schedule.api';
import { sortSessionsByShift } from './schedule.utils';
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
  for (const key in sessionsByDate) sortSessionsByShift(sessionsByDate[key]);

  return {
    sessions: data ?? [],
    sessionsByDate,
    isLoading,
  };
}
