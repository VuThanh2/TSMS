import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Table, Tag, Spin, Alert, Select, Empty } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';

import { getCourseByIdApi } from '@/modules/course-management/course-detail/course-detail.api';
import { useLecturerCourses } from '@/modules/enrollment-management/shared/useLecturerCourses';
import { useAttendance } from './useAttendance';
import type { AttendanceRecord, AttendanceStatus } from './attendance.types';

const STATUS_CONFIG: Record<AttendanceStatus, { label: string; color: string }> = {
  Present: { label: 'Có mặt', color: '#1E875F' },
  Absent: { label: 'Vắng', color: '#D7372C' },
  Excused: { label: 'Có phép', color: '#E5A20B' },
};

const STATUSES: AttendanceStatus[] = ['Present', 'Absent', 'Excused'];

export default function AttendancePage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const courseId = searchParams.get('courseId') ?? '';
  const sessionId = searchParams.get('sessionId') ?? '';

  // track row nào đang pending để hiển thị loading per-row
  const [pendingId, setPendingId] = useState<string | null>(null);

  const { courses, isLoading: coursesLoading } = useLecturerCourses();

  // Danh sách ClassSession của Course đang chọn (để đổ vào Select ca học)
  const courseDetail = useQuery({
    queryKey: ['course', courseId],
    queryFn: () => getCourseByIdApi(courseId),
    select: (res) => res.data,
    enabled: !!courseId,
  });

  const sessions = courseDetail.data?.classSessions ?? [];
  const session = sessions.find((s) => s.classSessionId === sessionId);

  const { attendances, isLoading, updateAttendance } = useAttendance(courseId, sessionId);

  function selectCourse(id: string) {
    setSearchParams(id ? { courseId: id } : {});
  }

  function selectSession(id: string) {
    setSearchParams(id ? { courseId, sessionId: id } : { courseId });
  }

  function handleStatusChange(attendanceId: string, status: AttendanceStatus) {
    setPendingId(attendanceId);
    updateAttendance.mutate(
      { attendanceId, status },
      { onSettled: () => setPendingId(null) },
    );
  }

  const columns: ColumnsType<AttendanceRecord> = [
    {
      title: 'Sinh viên',
      dataIndex: 'studentFullName',
      key: 'name',
      render: (v: string) => <span className="font-semibold">{v}</span>,
    },
    {
      title: 'Trạng thái',
      key: 'status',
      render: (_, record) => {
        const isUpdating = pendingId === record.attendanceId;
        return (
          <div className="flex gap-2">
            {STATUSES.map((s) => {
              const cfg = STATUS_CONFIG[s];
              const isActive = record.attendanceStatus === s;
              return (
                <button
                  key={s}
                  disabled={isUpdating || !!session?.isCancelled}
                  onClick={() => handleStatusChange(record.attendanceId, s)}
                  className={`cursor-pointer rounded-md border px-3 py-1 text-[13px] font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
                    isActive
                      ? 'border-transparent text-white'
                      : 'border-border bg-transparent text-text-muted hover:border-current'
                  }`}
                  style={isActive ? { backgroundColor: cfg.color, borderColor: cfg.color } : {}}
                >
                  {cfg.label}
                </button>
              );
            })}
            {isUpdating && <Spin size="small" />}
          </div>
        );
      },
    },
    {
      title: 'Ghi nhận lúc',
      dataIndex: 'markedAt',
      key: 'markedAt',
      align: 'right',
      render: (v: string | null) =>
        v ? (
          <span className="font-mono text-[13px] text-text-muted">{v}</span>
        ) : (
          <span className="text-[13px] text-text-muted">—</span>
        ),
    },
  ];

  function sessionLabel(s: (typeof sessions)[number]) {
    const ca = s.sessionType === 'Morning' ? 'Sáng (AM)' : 'Chiều (PM)';
    const cancelled = s.isCancelled ? ' · Đã hủy' : '';
    return `${s.sessionDate} · ${s.dayOfWeek} · ${ca}${cancelled}`;
  }

  return (
    <div className="max-w-[900px] p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Điểm danh</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Chọn khóa học và ca học để điểm danh từng sinh viên.
        </p>
      </div>

      {/* Selectors */}
      <div className="mb-6 flex flex-wrap gap-4">
        <div className="min-w-[280px] flex-1">
          <label className="mb-1.5 block text-[13px] font-semibold text-text-muted">Khóa học</label>
          <Select
            value={courseId || undefined}
            onChange={selectCourse}
            loading={coursesLoading}
            placeholder="Chọn khóa học…"
            size="large"
            className="w-full"
            showSearch
            options={courses.map((c) => ({ value: c.courseId, label: c.name }))}
          />
        </div>
        <div className="min-w-[280px] flex-1">
          <label className="mb-1.5 block text-[13px] font-semibold text-text-muted">Ca học</label>
          <Select
            value={sessionId || undefined}
            onChange={selectSession}
            loading={courseDetail.isLoading}
            disabled={!courseId}
            placeholder="Chọn ca học…"
            size="large"
            className="w-full"
            showSearch
            options={sessions.map((s) => ({ value: s.classSessionId, label: sessionLabel(s) }))}
          />
        </div>
      </div>

      {!courseId || !sessionId ? (
        <Empty description="Chọn khóa học và ca học để bắt đầu điểm danh." className="py-16" />
      ) : (
        <>
          {session?.isCancelled && (
            <div className="mb-6">
              <div className="mb-3 flex items-center gap-3">
                <Tag color="red">Buổi học đã bị hủy</Tag>
              </div>
              <Alert
                type="error"
                title="Buổi học này đã bị hủy bởi Admin. Không thể cập nhật điểm danh."
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
