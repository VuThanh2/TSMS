import { Spin } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { getStudentScheduleApi } from './schedule.api';
import ScheduleCalendar from './ScheduleCalendar';
import { sortSessionsByShift } from './schedule.utils';
import type { StudentScheduleSession } from './schedule.types';

// Màu theo ca (AM/PM) — thống nhất với lịch Lecturer, không phân biệt theo
// trạng thái điểm danh nữa để 2 role nhìn nhất quán.
const AM_BG = '#EBF3FC';
const AM_TEXT = '#2E73C4';
const PM_BG = '#FEF0EE';
const PM_TEXT = '#F45D48';
// Ca bị hủy: tô xám + gạch ngang, khác hẳn AM/PM để nhận biết ngay.
const CANCELLED_BG = '#EFECE8';
const CANCELLED_TEXT = '#9A9691';

function SessionChip({ session }: { session: StudentScheduleSession }) {
  const isAM = session.sessionType === 'Morning';
  const cancelled = session.isCancelled;
  const bg = cancelled ? CANCELLED_BG : isAM ? AM_BG : PM_BG;
  const color = cancelled ? CANCELLED_TEXT : isAM ? AM_TEXT : PM_TEXT;

  return (
    <div
      title={`${session.courseName} — ${isAM ? 'Morning' : 'Afternoon'}${cancelled ? ' · Cancelled' : ''}`}
      style={{ background: bg, color }}
      className="mb-0.5 flex w-full items-center gap-1 overflow-hidden rounded px-1.5 py-[3px]"
    >
      <span className="h-1.5 w-1.5 flex-none rounded-full" style={{ background: color }} />
      <span className={`min-w-0 flex-1 truncate text-[11px] font-semibold leading-tight ${cancelled ? 'line-through' : ''}`}>
        {session.courseName}
      </span>
      <span className="flex-none text-[10px] font-bold opacity-70">{isAM ? 'AM' : 'PM'}</span>
    </div>
  );
}

export default function StudentSchedulePage() {
  const { data, isLoading } = useQuery({
    queryKey: ['student-schedule'],
    queryFn: getStudentScheduleApi,
    select: (res) => res.data.items ?? [],
  });

  const sessions = data ?? [];

  // Group theo ngày + sắp AM lên trên PM dưới trong cùng 1 ngày
  const sessionsByDate = sessions.reduce<Record<string, StudentScheduleSession[]>>((acc, s) => {
    (acc[s.sessionDate] ??= []).push(s);
    return acc;
  }, {});
  for (const key in sessionsByDate) sortSessionsByShift(sessionsByDate[key]);

  return (
    <div className="p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">My Schedule</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Your class schedule — switch months to view; colors show morning/afternoon shift.
        </p>
      </div>

      <div className="mb-5 flex gap-6">
        <div className="flex items-center gap-2 text-[13px] text-text-secondary">
          <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: AM_TEXT }} />
          Morning (AM)
        </div>
        <div className="flex items-center gap-2 text-[13px] text-text-secondary">
          <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: PM_TEXT }} />
          Afternoon (PM)
        </div>
        <div className="flex items-center gap-2 text-[13px] text-text-secondary">
          <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: CANCELLED_TEXT }} />
          <span className="line-through">Cancelled</span>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center pt-20">
          <Spin size="large" />
        </div>
      ) : (
        <ScheduleCalendar<StudentScheduleSession>
          sessionsByDate={sessionsByDate}
          getKey={(s) => s.classSessionId}
          renderChip={(s) => <SessionChip session={s} />}
        />
      )}
    </div>
  );
}
