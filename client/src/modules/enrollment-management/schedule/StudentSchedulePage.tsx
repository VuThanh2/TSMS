import { Spin } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { getStudentScheduleApi } from './schedule.api';
import ScheduleCalendar from './ScheduleCalendar';
import type { StudentScheduleSession, StudentAttendanceStatus } from './schedule.types';

const ATTENDANCE_CONFIG: Record<
  Exclude<StudentAttendanceStatus, null> | 'pending',
  { color: string; bg: string; label: string }
> = {
  Present: { color: '#1E875F', bg: '#EBF7F1', label: 'Có mặt' },
  Absent: { color: '#D7372C', bg: '#FEF0EE', label: 'Vắng' },
  Excused: { color: '#E5A20B', bg: '#FDF6E7', label: 'Có phép' },
  pending: { color: '#8A847E', bg: '#F5F4F3', label: 'Chưa điểm danh' },
};

function getConfig(status: StudentAttendanceStatus) {
  return ATTENDANCE_CONFIG[status ?? 'pending'];
}

function SessionChip({ session }: { session: StudentScheduleSession }) {
  const cfg = getConfig(session.attendanceStatus);
  const isAM = session.sessionType === 'Morning';

  return (
    <div
      title={`${session.courseName} — ${isAM ? 'Sáng' : 'Chiều'} · ${cfg.label}`}
      style={{ background: cfg.bg, color: cfg.color }}
      className="mb-0.5 flex w-full items-center gap-1 overflow-hidden rounded px-1.5 py-[3px]"
    >
      <span className="h-1.5 w-1.5 flex-none rounded-full" style={{ background: cfg.color }} />
      <span className="min-w-0 flex-1 truncate text-[11px] font-semibold leading-tight">
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

  const sessionsByDate = sessions.reduce<Record<string, StudentScheduleSession[]>>((acc, s) => {
    (acc[s.sessionDate] ??= []).push(s);
    return acc;
  }, {});

  return (
    <div className="p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">My Schedule</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Lịch học của bạn — chuyển tháng để xem, màu thể hiện trạng thái điểm danh.
        </p>
      </div>

      <div className="mb-5 flex flex-wrap gap-5">
        {Object.entries(ATTENDANCE_CONFIG).map(([key, cfg]) => (
          <div key={key} className="flex items-center gap-2 text-[13px] text-text-secondary">
            <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: cfg.color }} />
            {cfg.label}
          </div>
        ))}
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
