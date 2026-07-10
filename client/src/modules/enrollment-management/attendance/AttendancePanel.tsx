import { Fragment, useMemo, useState } from 'react';
import { Alert, Empty, Spin, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import dayjs, { type Dayjs } from 'dayjs';

import { getCourseByIdApi } from '@/modules/course-management/course-detail/course-detail.api';
import { getSessionState, SESSION_STATE_LABEL } from '@/modules/course-management/shared/session-status';
import type { ClassSession } from '@/modules/course-management/shared/course.types';
import { useAttendance } from './useAttendance';
import type { AttendanceRecord, AttendanceStatus } from './attendance.types';

const STATUS_CONFIG: Record<AttendanceStatus, { label: string; color: string }> = {
  Present: { label: 'Present', color: '#1E875F' },
  Excused: { label: 'Excused', color: '#E5A20B' },
  Absent: { label: 'Absent', color: '#D7372C' },
};

const STATUSES: AttendanceStatus[] = ['Present', 'Excused', 'Absent'];
const DAY_SHORT = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];

// Tuần bắt đầu từ Thứ Hai (khớp convention ScheduleCalendar/CourseWeeklySchedule)
function startOfWeek(d: Dayjs) {
  const diff = (d.day() + 6) % 7;
  return d.subtract(diff, 'day').startOf('day');
}

function sessionLabel(s: ClassSession) {
  const shift = s.sessionType === 'Morning' ? 'AM' : 'PM';
  return `${dayjs(s.sessionDate).format('ddd, MMM D')} · ${shift}`;
}

// Nội dung tab Attendance trong CourseDetailPage — chọn buổi trong tuần rồi điểm danh.
// courseId đến từ route (CourseDetail); initialSessionId cho phép deep-link từ Schedule.
export default function AttendancePanel({
  courseId,
  initialSessionId = '',
}: {
  courseId: string;
  initialSessionId?: string;
}) {
  const [sessionId, setSessionId] = useState(initialSessionId);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [weekOffset, setWeekOffset] = useState(0);

  // Dùng chung queryKey ['course', courseId] với CourseDetail → hit cache, không refetch.
  const courseDetail = useQuery({
    queryKey: ['course', courseId],
    queryFn: () => getCourseByIdApi(courseId),
    select: (res) => res.data,
    enabled: !!courseId,
  });

  const sessions = courseDetail.data?.classSessions ?? [];
  const session = sessions.find((s) => s.classSessionId === sessionId);

  const { attendances, isLoading, updateAttendance } = useAttendance(courseId, sessionId);

  // Tuần mặc định: ưu tiên tuần chứa buổi deep-link (từ Schedule), rồi buổi sắp tới gần nhất,
  // cuối cùng là buổi đầu — để mở đúng tuần và highlight được buổi vừa chọn.
  const defaultAnchor = useMemo(() => {
    const deepLinked = initialSessionId
      ? sessions.find((s) => s.classSessionId === initialSessionId)
      : undefined;
    const anchor = deepLinked ?? sessions.find((s) => !s.isPast) ?? sessions[0];
    return anchor ? dayjs(anchor.sessionDate) : dayjs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [courseId, sessions.length, initialSessionId]);

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

  function handleStatusChange(attendanceId: string, status: AttendanceStatus) {
    setPendingId(attendanceId);
    updateAttendance.mutate(
      { attendanceId, status },
      {
        onSuccess: () => {
          setSavedId(attendanceId);
          setTimeout(() => setSavedId((cur) => (cur === attendanceId ? null : cur)), 2000);
        },
        onSettled: () => setPendingId(null),
      },
    );
  }

  const columns: ColumnsType<AttendanceRecord> = [
    {
      title: 'Student',
      dataIndex: 'studentFullName',
      key: 'name',
      render: (v: string) => <span className="text-[15px] font-semibold">{v}</span>,
    },
    {
      title: '',
      key: 'status',
      render: (_, record) => {
        const isUpdating = pendingId === record.attendanceId;
        return (
          <div className="flex items-center gap-3">
            <div className="flex flex-none overflow-hidden rounded-[9px] border border-border-input">
              {STATUSES.map((s) => {
                const cfg = STATUS_CONFIG[s];
                const isActive = record.attendanceStatus === s;
                return (
                  <button
                    key={s}
                    disabled={isUpdating || !!session?.isCancelled}
                    onClick={() => handleStatusChange(record.attendanceId, s)}
                    className={`h-9 cursor-pointer border-0 px-3.5 text-[13px] font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
                      isActive ? 'text-white' : 'bg-transparent text-text-muted hover:text-text'
                    }`}
                    style={isActive ? { backgroundColor: cfg.color } : {}}
                  >
                    {cfg.label}
                  </button>
                );
              })}
            </div>
            {isUpdating && <Spin size="small" />}
            {savedId === record.attendanceId && (
              <span className="text-[13px] font-semibold text-[#1E875F]">✓ Saved</span>
            )}
          </div>
        );
      },
    },
  ];

  if (courseDetail.isLoading) {
    return <div className="flex justify-center py-16"><Spin size="large" /></div>;
  }

  return (
    <div className="max-w-[880px]">
      <p className="m-0 mb-4 text-[14px] text-text-secondary">
        Choose a session in the week below, then mark each student.
      </p>

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

      <div className="mb-4 overflow-x-auto rounded-xl border border-border bg-white p-4 shadow-sm">
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

          {(['Morning', 'Afternoon'] as const).map((type) => (
            <Fragment key={type}>
              <div className="text-[12px] font-bold text-text-muted">
                {type === 'Morning' ? 'AM' : 'PM'}
              </div>
              {weekDays.map((d) => {
                const s = sessionOn(d, type);
                const key = `${type}-${d.format('YYYY-MM-DD')}`;
                if (!s) {
                  return <div key={key} className="h-9 rounded-lg border border-dashed border-border" />;
                }
                const isSelected = s.classSessionId === sessionId;
                return (
                  <button
                    key={key}
                    onClick={() => setSessionId(s.classSessionId)}
                    title={sessionLabel(s)}
                    className={`h-9 cursor-pointer rounded-lg border text-[11px] font-semibold transition-colors ${
                      isSelected
                        ? 'border-transparent bg-primary text-white'
                        : s.isCancelled
                          ? 'border-border-input bg-transparent text-text-muted line-through'
                          : getSessionState(s) === 'today'
                            ? 'border-primary bg-transparent text-primary'
                            : 'border-border-input bg-transparent text-text-secondary hover:border-primary hover:text-primary'
                    }`}
                  >
                    {SESSION_STATE_LABEL[getSessionState(s)]}
                  </button>
                );
              })}
            </Fragment>
          ))}
        </div>
      </div>

      {!sessionId || !session ? (
        <Empty description="Select a session above to mark attendance." className="py-16" />
      ) : (
        <>
          <p className="m-0 mb-[22px] text-[14px] text-text-muted">
            Marking <strong className="text-text-secondary">{sessionLabel(session)}</strong> · each change saves on its own.
          </p>

          {session.isCancelled && (
            <div className="mb-4">
              <Alert
                type="error"
                title="This session was cancelled by an Admin. Attendance can no longer be updated."
                showIcon
              />
            </div>
          )}

          <Table<AttendanceRecord>
            columns={columns}
            dataSource={attendances}
            rowKey="attendanceId"
            loading={isLoading}
            pagination={false}
          />
        </>
      )}
    </div>
  );
}
