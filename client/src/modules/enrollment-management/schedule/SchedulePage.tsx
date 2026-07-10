import { useNavigate } from 'react-router-dom';
import { Spin } from 'antd';

import { useSchedule } from './useSchedule';
import ScheduleCalendar from './ScheduleCalendar';
import type { LecturerScheduleSession } from './schedule.types';

const AM_BG = '#EBF3FC';
const AM_TEXT = '#2E73C4';
const PM_BG = '#FEF0EE';
const PM_TEXT = '#F45D48';
// Ca bị hủy: tô xám + gạch ngang, khác hẳn AM/PM để nhận biết ngay.
const CANCELLED_BG = '#EFECE8';
const CANCELLED_TEXT = '#9A9691';

function SessionChip({
  session,
  onClick,
}: {
  session: LecturerScheduleSession;
  onClick: () => void;
}) {
  const isAM = session.sessionType === 'Morning';
  const cancelled = session.isCancelled;
  const bg = cancelled ? CANCELLED_BG : isAM ? AM_BG : PM_BG;
  const color = cancelled ? CANCELLED_TEXT : isAM ? AM_TEXT : PM_TEXT;

  return (
    <button
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      title={`${session.courseName} — ${isAM ? 'Morning' : 'Afternoon'}${cancelled ? ' · Cancelled' : ' · click to mark attendance'}`}
      style={{ background: bg, color }}
      className="mb-0.5 flex w-full cursor-pointer items-center gap-1 overflow-hidden rounded border-none px-1.5 py-[3px] text-left transition-opacity hover:opacity-80"
    >
      <span className="h-1.5 w-1.5 flex-none rounded-full" style={{ background: color }} />
      <span className={`min-w-0 flex-1 truncate text-[11px] font-semibold leading-tight ${cancelled ? 'line-through' : ''}`}>
        {session.courseName}
      </span>
      <span className="flex-none text-[10px] font-bold opacity-70">{isAM ? 'AM' : 'PM'}</span>
    </button>
  );
}

export default function SchedulePage() {
  const navigate = useNavigate();
  const { sessionsByDate, isLoading } = useSchedule();

  return (
    <div className="p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">My Schedule</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Your teaching schedule — switch months to view; click a session to open attendance.
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
        <ScheduleCalendar<LecturerScheduleSession>
          sessionsByDate={sessionsByDate}
          getKey={(s) => s.classSessionId}
          renderChip={(s) => (
            <SessionChip
              session={s}
              onClick={() =>
                navigate(
                  `/lecturer/attendance?courseId=${s.courseId}&sessionId=${s.classSessionId}`,
                )
              }
            />
          )}
        />
      )}
    </div>
  );
}
