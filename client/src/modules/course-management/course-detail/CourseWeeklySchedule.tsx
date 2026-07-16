import { useMemo, useState } from 'react';
import dayjs, { type Dayjs } from 'dayjs';

import type { ClassSession } from '@/modules/course-management/shared/course.types';
import {
  getSessionState,
  SESSION_STATE_LABEL,
  SESSION_STATE_STYLE,
  type SessionState,
} from '@/modules/course-management/shared/session-status';
import { formatAttendanceCell } from '@/modules/enrollment-management/attendance/attendance-cell';
import { useCourseAttendanceSummary } from '@/modules/enrollment-management/attendance/useCourseAttendanceSummary';

const DAY_SHORT = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];
const LEGEND_STATES: SessionState[] = ['upcoming', 'today', 'past', 'cancelled'];

// Tuần bắt đầu từ Thứ Hai (khớp convention ở AttendancePage/ScheduleCalendar).
function startOfWeek(d: Dayjs) {
  const diff = (d.day() + 6) % 7;
  return d.subtract(diff, 'day').startOf('day');
}

// Lịch dạy read-only của 1 khóa học, group theo TUẦN (không theo tháng như Schedule Page),
// điều hướng tuần bằng mũi tên. Ca bị hủy tô xám + gạch ngang để phân biệt.
// Buổi đã qua hiện số có mặt (12/15) thay vì nhãn "Past" — chỉ Lecturer, vì endpoint
// attendance-summary là Lecturer-only.
export default function CourseWeeklySchedule({
  sessions,
  courseId,
}: {
  sessions: ClassSession[];
  courseId: string;
}) {
  const [weekOffset, setWeekOffset] = useState(0);
  const attendanceSummary = useCourseAttendanceSummary(courseId);

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
    const shift = type === 'Morning' ? 'Morning' : 'Afternoon';
    return weekDays.map((d) => {
      const s = sessionOn(d, type);
      const key = `${type}-${d.format('YYYY-MM-DD')}`;
      if (!s) {
        return <div key={key} className="h-10 rounded-lg border border-dashed border-border" />;
      }
      // Nội dung ô = TRẠNG THÁI buổi học (Upcoming/Today/Past/Cancelled) — có ý nghĩa
      // hơn nhãn AM/PM cũ (ca đã nằm ở nhãn hàng, ngày đã nằm ở tiêu đề cột).
      // Riêng buổi đã qua: thay nhãn "Past" bằng số có mặt, cụ thể hơn hẳn.
      const state = getSessionState(s);
      const st = SESSION_STATE_STYLE[state];

      // Buổi past chưa có summary (course chưa ai enroll, hoặc query chưa về) → giữ nhãn cũ.
      const summary = state === 'past' ? attendanceSummary.get(s.classSessionId) : undefined;
      const cell = summary ? formatAttendanceCell(summary) : undefined;

      return (
        <div
          key={key}
          title={cell ? `${shift} · ${cell.title}` : `${shift} · ${SESSION_STATE_LABEL[state]}`}
          style={{ background: st.bg, color: st.color }}
          className={`flex h-10 items-center justify-center rounded-lg font-semibold ${
            cell ? 'font-mono text-[12px]' : 'text-[11.5px]'
          } ${state === 'cancelled' ? 'line-through' : ''}`}
        >
          {cell ? cell.label : SESSION_STATE_LABEL[state]}
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
      <div className="overflow-x-auto rounded-xl border border-border bg-white p-4 shadow-sm">
        <div className="grid items-center gap-2" style={{ gridTemplateColumns: '44px repeat(7, 1fr)', minWidth: 520 }}>
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

      {/* Chú thích theo trạng thái */}
      <div className="mt-3 flex flex-wrap gap-4">
        {LEGEND_STATES.map((state) => (
          <div key={state} className="flex items-center gap-2 text-[13px] text-text-secondary">
            <span
              className="inline-block h-2.5 w-2.5 rounded-full"
              style={{ background: SESSION_STATE_STYLE[state].color }}
            />
            <span className={state === 'cancelled' ? 'line-through' : ''}>
              {SESSION_STATE_LABEL[state]}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
