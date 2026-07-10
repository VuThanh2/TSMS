import { useMemo, useState } from 'react';
import dayjs, { type Dayjs } from 'dayjs';

import type { ClassSession } from '@/modules/course-management/shared/course.types';

const DAY_SHORT = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];

// Màu theo ca — đồng bộ với lịch Lecturer/Student (SchedulePage) để nhìn nhất quán.
const AM_BG = '#EBF3FC';
const AM_TEXT = '#2E73C4';
const PM_BG = '#FEF0EE';
const PM_TEXT = '#F45D48';

// Tuần bắt đầu từ Thứ Hai (khớp convention ở AttendancePage/ScheduleCalendar).
function startOfWeek(d: Dayjs) {
  const diff = (d.day() + 6) % 7;
  return d.subtract(diff, 'day').startOf('day');
}

// Lịch dạy read-only của 1 khóa học, group theo TUẦN (không theo tháng như Schedule Page),
// điều hướng tuần bằng mũi tên. Ca bị hủy tô xám + gạch ngang để phân biệt.
export default function CourseWeeklySchedule({ sessions }: { sessions: ClassSession[] }) {
  const [weekOffset, setWeekOffset] = useState(0);

  // Tuần mặc định: chứa buổi sắp tới gần nhất, hoặc buổi đầu tiên nếu tất cả đã qua.
  const defaultAnchor = useMemo(() => {
    const firstUpcoming = sessions.find((s) => !s.isPast) ?? sessions[0];
    return firstUpcoming ? dayjs(firstUpcoming.sessionDate) : dayjs();
  }, [sessions]);

  const weekStart = startOfWeek(defaultAnchor).add(weekOffset * 7, 'day');
  const weekDays = useMemo(
    () => Array.from({ length: 7 }, (_, i) => weekStart.add(i, 'day')),
    [weekStart],
  );
  const weekLabel = `${weekStart.format('MMM D')} – ${weekStart.add(6, 'day').format('MMM D, YYYY')}`;

  function sessionOn(day: Dayjs, sessionType: string) {
    const dateStr = day.format('YYYY-MM-DD');
    return sessions.find((s) => s.sessionDate === dateStr && s.sessionType === sessionType);
  }

  function renderRow(type: 'Morning' | 'Afternoon') {
    const isAM = type === 'Morning';
    return weekDays.map((d) => {
      const s = sessionOn(d, type);
      const key = `${type}-${d.format('YYYY-MM-DD')}`;
      if (!s) {
        return <div key={key} className="h-10 rounded-lg border border-dashed border-border" />;
      }
      if (s.isCancelled) {
        return (
          <div
            key={key}
            title="Session cancelled"
            className="flex h-10 items-center justify-center rounded-lg bg-bg-card text-[12px] font-semibold text-text-muted line-through"
          >
            {isAM ? 'AM' : 'PM'}
          </div>
        );
      }
      return (
        <div
          key={key}
          title={isAM ? 'Morning' : 'Afternoon'}
          style={{ background: isAM ? AM_BG : PM_BG, color: isAM ? AM_TEXT : PM_TEXT }}
          className="flex h-10 items-center justify-center rounded-lg text-[12px] font-semibold"
        >
          {isAM ? 'AM' : 'PM'}
        </div>
      );
    });
  }

  return (
    <div>
      {/* Điều hướng tuần */}
      <div className="mb-3.5 flex items-center justify-between">
        <button
          onClick={() => setWeekOffset((o) => o - 1)}
          aria-label="Previous week"
          className="flex h-9 w-9 cursor-pointer items-center justify-center rounded-lg border border-border-input bg-white text-[16px] font-semibold text-text-secondary"
        >
          ‹
        </button>
        <div className="text-[14px] font-semibold text-text-secondary">{weekLabel}</div>
        <button
          onClick={() => setWeekOffset((o) => o + 1)}
          aria-label="Next week"
          className="flex h-9 w-9 cursor-pointer items-center justify-center rounded-lg border border-border-input bg-white text-[16px] font-semibold text-text-secondary"
        >
          ›
        </button>
      </div>

      {/* Lưới tuần */}
      <div className="rounded-xl border border-border bg-white p-4 shadow-sm">
        <div className="grid items-center gap-2" style={{ gridTemplateColumns: '44px repeat(7, 1fr)' }}>
          <div />
          {weekDays.map((d) => (
            <div key={d.format('YYYY-MM-DD')} className="text-center">
              <div className="text-[11px] font-semibold uppercase tracking-wide text-text-muted">
                {DAY_SHORT[d.day() === 0 ? 6 : d.day() - 1]}
              </div>
              <div className="font-mono text-[13px] font-semibold">{d.format('D')}</div>
            </div>
          ))}

          <div className="text-[12px] font-bold text-text-muted">AM</div>
          {renderRow('Morning')}

          <div className="text-[12px] font-bold text-text-muted">PM</div>
          {renderRow('Afternoon')}
        </div>
      </div>

      {/* Chú thích */}
      <div className="mt-3 flex flex-wrap gap-5">
        <div className="flex items-center gap-2 text-[13px] text-text-secondary">
          <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: AM_TEXT }} />
          Morning (AM)
        </div>
        <div className="flex items-center gap-2 text-[13px] text-text-secondary">
          <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: PM_TEXT }} />
          Afternoon (PM)
        </div>
        <div className="flex items-center gap-2 text-[13px] text-text-secondary">
          <span className="inline-block h-2.5 w-2.5 rounded-full bg-text-muted" />
          <span className="line-through">Cancelled</span>
        </div>
      </div>
    </div>
  );
}
