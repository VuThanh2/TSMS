import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Button, Table, Alert, Empty, Spin } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import dayjs, { type Dayjs } from 'dayjs';

import StatusTag from '@/shared/components/StatusTag';
import { getCourseByIdApi } from '@/modules/course-management/course-detail/course-detail.api';
import { useLecturerCourses } from '@/modules/enrollment-management/shared/useLecturerCourses';
import type { CourseListItem, ClassSession } from '@/modules/course-management/shared/course.types';
import { useAttendance } from './useAttendance';
import type { AttendanceRecord, AttendanceStatus } from './attendance.types';

const STATUS_CONFIG: Record<AttendanceStatus, { label: string; color: string }> = {
  Present: { label: 'Present', color: '#1E875F' },
  Excused: { label: 'Excused', color: '#E5A20B' },
  Absent: { label: 'Absent', color: '#D7372C' },
};

const STATUSES: AttendanceStatus[] = ['Present', 'Excused', 'Absent'];
const DAY_SHORT = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];

// Tuần bắt đầu từ Thứ Hai (không dùng plugin isoWeek — khớp convention ở ScheduleCalendar.tsx)
function startOfWeek(d: Dayjs) {
  const diff = (d.day() + 6) % 7;
  return d.subtract(diff, 'day').startOf('day');
}

function sessionLabel(s: ClassSession) {
  const shift = s.sessionType === 'Morning' ? 'AM' : 'PM';
  return `${dayjs(s.sessionDate).format('ddd, MMM D')} · ${shift}`;
}

// Ghi chú: API Contract Mapping không có endpoint riêng cho "Attendance list" kèm số buổi học
// — màn hình chọn khóa học tái dùng GET /api/courses/my-courses. Cột "Sessions" hiện "—"
// thay vì tự tính (tránh gọi thêm 1 API/khóa học chỉ để đếm buổi).
const courseListColumns: ColumnsType<CourseListItem> = [
  {
    title: 'Course',
    dataIndex: 'name',
    key: 'name',
    render: (v: string) => <span className="text-[15px] font-semibold">{v}</span>,
  },
  {
    title: 'Status',
    dataIndex: 'status',
    key: 'status',
    render: (status: CourseListItem['status']) => <StatusTag status={status} />,
  },
  {
    title: 'Sessions',
    key: 'sessions',
    render: () => <span className="text-[14px] text-text-muted">—</span>,
  },
];

export default function AttendancePage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const courseId = searchParams.get('courseId') ?? '';
  const sessionId = searchParams.get('sessionId') ?? '';

  // track row nào đang pending để hiển thị loading per-row
  const [pendingId, setPendingId] = useState<string | null>(null);
  // hiện "✓ Saved" tạm thời sau khi lưu thành công
  const [savedId, setSavedId] = useState<string | null>(null);
  // Số tuần lệch so với tuần "mặc định" (tuần chứa buổi học đầu tiên) — reset về 0 khi đổi khóa học
  const [weekOffset, setWeekOffset] = useState(0);
  const [prevCourseId, setPrevCourseId] = useState(courseId);
  if (courseId !== prevCourseId) {
    setPrevCourseId(courseId);
    setWeekOffset(0);
  }

  const { courses, isLoading: coursesLoading } = useLecturerCourses();
  const selectedCourse = courses.find((c) => c.courseId === courseId);

  const courseDetail = useQuery({
    queryKey: ['course', courseId],
    queryFn: () => getCourseByIdApi(courseId),
    select: (res) => res.data,
    enabled: !!courseId,
  });

  const sessions = courseDetail.data?.classSessions ?? [];
  const session = sessions.find((s) => s.classSessionId === sessionId);

  const { attendances, isLoading, updateAttendance } = useAttendance(courseId, sessionId);

  // Tuần mặc định: chứa buổi học sắp tới gần nhất, hoặc buổi đầu tiên nếu tất cả đã qua
  const defaultAnchor = useMemo(() => {
    const firstUpcoming = sessions.find((s) => !s.isPast) ?? sessions[0];
    return firstUpcoming ? dayjs(firstUpcoming.sessionDate) : dayjs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [courseId, sessions.length]);

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

  function selectCourse(id: string) {
    setSearchParams({ courseId: id });
  }

  function backToList() {
    setSearchParams({});
  }

  function selectSession(id: string) {
    setSearchParams({ courseId, sessionId: id });
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

  // ---- LIST VIEW: chọn khóa học ----
  if (!courseId) {
    return (
      <div className="p-10 px-12">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Attendance</h1>
        <p className="m-0 mb-7 text-[15px] text-text-secondary">
          Choose a course, then a session, to mark attendance.
        </p>

        {coursesLoading ? (
          <div className="flex justify-center pt-16"><Spin size="large" /></div>
        ) : courses.length === 0 ? (
          <Empty description="No active courses to mark yet." className="py-16" />
        ) : (
          <Table<CourseListItem>
            columns={courseListColumns}
            dataSource={courses}
            rowKey="courseId"
            pagination={false}
            onRow={(record) => ({
              onClick: () => selectCourse(record.courseId),
              style: { cursor: 'pointer' },
            })}
          />
        )}
      </div>
    );
  }

  // ---- MARK VIEW: chọn ca học trong tuần + điểm danh ----
  return (
    <div className="max-w-[880px] p-10 px-12">
      <Button type="text" icon={<ArrowLeftOutlined />} onClick={backToList} className="mb-4 p-0 text-text-secondary">
        Back to attendance
      </Button>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <h1 className="m-0 text-[28px] font-bold tracking-tight">{selectedCourse?.name}</h1>
        {selectedCourse && <StatusTag status={selectedCourse.status} />}
      </div>

      <div className="mb-3.5 flex items-center justify-between">
        <button
          onClick={() => setWeekOffset((o) => o - 1)}
          className="flex h-9 w-9 cursor-pointer items-center justify-center rounded-lg border border-border-input bg-white text-[16px] font-semibold text-text-secondary"
        >
          ‹
        </button>
        <div className="text-[14px] font-semibold text-text-secondary">{weekLabel}</div>
        <button
          onClick={() => setWeekOffset((o) => o + 1)}
          className="flex h-9 w-9 cursor-pointer items-center justify-center rounded-lg border border-border-input bg-white text-[16px] font-semibold text-text-secondary"
        >
          ›
        </button>
      </div>

      <div className="mb-4 rounded-xl border border-border bg-white p-4 shadow-sm">
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
          {weekDays.map((d) => {
            const s = sessionOn(d, 'Morning');
            const isSelected = s?.classSessionId === sessionId;
            return s ? (
              <button
                key={`am-${d.format('YYYY-MM-DD')}`}
                onClick={() => selectSession(s.classSessionId)}
                className={`h-9 cursor-pointer rounded-lg border font-mono text-[13px] font-semibold transition-colors ${
                  isSelected
                    ? 'border-transparent bg-primary text-white'
                    : s.isCancelled
                      ? 'border-border-input bg-transparent text-text-muted line-through'
                      : 'border-border-input bg-transparent text-text-secondary hover:border-primary hover:text-primary'
                }`}
              >
                {d.format('D')}
              </button>
            ) : (
              <div key={`am-${d.format('YYYY-MM-DD')}`} className="h-9 rounded-lg border border-dashed border-border" />
            );
          })}

          <div className="text-[12px] font-bold text-text-muted">PM</div>
          {weekDays.map((d) => {
            const s = sessionOn(d, 'Afternoon');
            const isSelected = s?.classSessionId === sessionId;
            return s ? (
              <button
                key={`pm-${d.format('YYYY-MM-DD')}`}
                onClick={() => selectSession(s.classSessionId)}
                className={`h-9 cursor-pointer rounded-lg border font-mono text-[13px] font-semibold transition-colors ${
                  isSelected
                    ? 'border-transparent bg-primary text-white'
                    : s.isCancelled
                      ? 'border-border-input bg-transparent text-text-muted line-through'
                      : 'border-border-input bg-transparent text-text-secondary hover:border-primary hover:text-primary'
                }`}
              >
                {d.format('D')}
              </button>
            ) : (
              <div key={`pm-${d.format('YYYY-MM-DD')}`} className="h-9 rounded-lg border border-dashed border-border" />
            );
          })}
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
                message="This session was cancelled by an Admin. Attendance can no longer be updated."
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
