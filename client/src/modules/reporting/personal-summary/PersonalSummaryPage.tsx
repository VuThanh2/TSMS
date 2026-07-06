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
    title: 'Khóa học',
    dataIndex: 'courseName',
    key: 'name',
    render: (v: string) => <span className="font-semibold">{v}</span>,
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    render: (v: PersonalSummaryItem['status']) => <StatusTag status={v} />,
  },
  {
    title: 'Điểm',
    dataIndex: 'grade',
    key: 'grade',
    align: 'right',
    render: (v: number | null) => (
      <span className={`font-mono text-[15px] font-semibold ${GRADE_COLOR(v)}`}>
        {v !== null ? v.toFixed(1) : 'Chưa có điểm'}
      </span>
    ),
  },
  {
    title: 'Có mặt',
    dataIndex: 'presentCount',
    key: 'present',
    align: 'center',
    render: (v: number) => <span className="font-mono font-semibold text-[#1E875F]">{v}</span>,
  },
  {
    title: 'Có phép',
    dataIndex: 'excusedCount',
    key: 'excused',
    align: 'center',
    render: (v: number) => <span className="font-mono font-semibold text-[#E5A20B]">{v}</span>,
  },
  {
    title: 'Vắng',
    dataIndex: 'absentCount',
    key: 'absent',
    align: 'center',
    render: (v: number) => <span className="font-mono font-semibold text-[#D7372C]">{v}</span>,
  },
  {
    title: 'Tỷ lệ đi học',
    dataIndex: 'attendanceRate',
    key: 'rate',
    align: 'right',
    render: (v: number) => (
      <span
        className={`font-mono text-[14px] font-semibold ${v >= 0.8 ? 'text-[#1E875F]' : v >= 0.6 ? 'text-[#E5A20B]' : 'text-[#D7372C]'}`}
      >
        {(v * 100).toFixed(0)}%
      </span>
    ),
  },
];

export default function PersonalSummaryPage() {
  const { summary, isLoading } = usePersonalSummary();

  if (isLoading) {
    return (
      <div className="flex justify-center pt-20">
        <Spin size="large" />
      </div>
    );
  }

  const gpa = summary?.overallGpa;
  const items = summary?.items ?? [];

  return (
    <div className="max-w-[1040px] p-10 px-12">
      {/* Header */}
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">My Summary</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Tổng hợp điểm và điểm danh của bạn. Cập nhật tự động khi giảng viên chấm điểm.
        </p>
      </div>

      {/* GPA Card */}
      <div className="mb-8 inline-flex items-center gap-6 rounded-2xl border border-border bg-white px-8 py-6 shadow-sm">
        <div>
          <div className="mb-1 text-[12px] font-semibold uppercase tracking-widest text-text-muted">
            Điểm trung bình chung (GPA)
          </div>
          {gpa !== null && gpa !== undefined ? (
            <div
              className={`font-mono text-[40px] font-bold leading-none ${GRADE_COLOR(gpa)}`}
            >
              {gpa.toFixed(2)}
            </div>
          ) : (
            <div className="font-mono text-[40px] font-bold leading-none text-text-muted">—</div>
          )}
          {gpa === null && (
            <p className="mt-1 text-[13px] text-text-muted">Chưa có điểm nào được ghi nhận.</p>
          )}
        </div>
      </div>

      {/* Detail table */}
      <h2 className="mb-4 text-[20px] font-semibold tracking-tight">Chi tiết theo khóa học</h2>
      <Table<PersonalSummaryItem>
        columns={columns}
        dataSource={items}
        rowKey="courseId"
        pagination={false}
      />
    </div>
  );
}
