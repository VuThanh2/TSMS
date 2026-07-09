import { Table, Spin } from 'antd';
import type { ColumnsType } from 'antd/es/table';

import StatusTag from '@/shared/components/StatusTag';
import type { PersonalSummaryItem } from '@/modules/reporting/shared/reporting.types';
import { usePersonalSummary } from './usePersonalSummary';

const GRADE_COLOR = (g: number | null) => {
  if (g === null) return 'text-text-muted';
  return g >= 5 ? 'text-[#1E875F]' : 'text-[#D7372C]';
};

const columns: ColumnsType<PersonalSummaryItem> = [
  {
    title: 'Course',
    dataIndex: 'courseName',
    key: 'name',
    render: (v: string) => <span className="font-semibold">{v}</span>,
  },
  {
    title: 'Status',
    dataIndex: 'status',
    key: 'status',
    render: (v: PersonalSummaryItem['status']) => <StatusTag status={v} />,
  },
  {
    title: 'Grade',
    dataIndex: 'grade',
    key: 'grade',
    align: 'right',
    render: (v: number | null) => (
      <span className={`font-mono text-[15px] font-semibold ${GRADE_COLOR(v)}`}>
        {v !== null ? v.toFixed(1) : 'Not graded yet'}
      </span>
    ),
  },
  {
    title: 'Attendance',
    dataIndex: 'attendanceRate',
    key: 'rate',
    align: 'right',
    render: (v: number) => (
      <span className="font-mono text-[14px] font-semibold text-text-secondary">
        {(v * 100).toFixed(0)}%
      </span>
    ),
  },
];

function StatTile({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: string;
}) {
  return (
    <div className="rounded-xl border border-border bg-white px-6 py-[22px] shadow-sm">
      <div className="mb-2.5 text-[12px] font-semibold uppercase tracking-wide text-text-muted">
        {label}
      </div>
      <div className="font-mono text-[34px] font-bold leading-none" style={{ color }}>
        {value}
      </div>
    </div>
  );
}

export default function PersonalSummaryPage() {
  const { summary, isLoading } = usePersonalSummary();

  if (isLoading) {
    return (
      <div className="flex justify-center pt-20">
        <Spin size="large" />
      </div>
    );
  }

  const gpa = summary?.overallGpa ?? null;
  const items = summary?.items ?? [];

  // Không tự tính lại từ totalSessions/presentCount — totalSessions đếm TOÀN BỘ
  // ClassSession của Course (kể cả buổi tương lai chưa diễn ra), tính thẳng ra tỷ lệ
  // sẽ bị lệch khi Course vừa bắt đầu. Lấy trung bình attendanceRate mỗi Course —
  // giá trị này Backend đã tính đúng theo rule "chỉ tính ca đã kết thúc".
  const attendanceRate =
    items.length > 0
      ? items.reduce((sum, i) => sum + i.attendanceRate, 0) / items.length
      : null;

  const gpaColor = gpa === null ? '#8A847E' : gpa >= 5 ? '#1E875F' : '#D7372C';

  return (
    <div className="max-w-[960px] p-10 px-12">
      {/* Header */}
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Personal summary</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Your grades and attendance across every course.
        </p>
      </div>

      {/* Stat tiles */}
      <div className="mb-7 grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatTile label="Overall GPA" value={gpa !== null ? gpa.toFixed(2) : '—'} color={gpaColor} />
        <StatTile
          label="Attendance rate"
          value={attendanceRate !== null ? `${(attendanceRate * 100).toFixed(0)}%` : '—'}
          color="#1E875F"
        />
        <StatTile label="Courses enrolled" value={String(items.length)} color="#1C1B1A" />
      </div>

      {/* Detail table */}
      <Table<PersonalSummaryItem>
        columns={columns}
        dataSource={items}
        rowKey="courseId"
        pagination={false}
      />
    </div>
  );
}
