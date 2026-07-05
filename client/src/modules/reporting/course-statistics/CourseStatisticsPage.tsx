import { useNavigate } from 'react-router-dom';
import { Table, Spin } from 'antd';
import ReactECharts from 'echarts-for-react';
import type { ColumnsType } from 'antd/es/table';

import StatusTag from '@/shared/components/StatusTag';
import type { CourseStatisticsItem } from '@/modules/reporting/shared/reporting.types';
import { useCourseStatistics } from './useCourseStatistics';

const columns: ColumnsType<CourseStatisticsItem> = [
  {
    title: 'Course',
    dataIndex: 'courseName',
    key: 'courseName',
    render: (name: string) => <span className="text-[15px] font-semibold">{name}</span>,
  },
  {
    title: 'Status',
    dataIndex: 'status',
    key: 'status',
    render: (status: CourseStatisticsItem['status']) => <StatusTag status={status} />,
  },
  {
    title: 'Enrolled',
    dataIndex: 'enrolledCount',
    key: 'enrolledCount',
    align: 'right',
    render: (v: number) => <span className="font-mono text-[14px] font-medium">{v}</span>,
  },
  {
    title: 'Avg score',
    dataIndex: 'averageScore',
    key: 'averageScore',
    align: 'right',
    render: (v: number | null) => (
      <span className={`font-mono text-[14px] font-semibold ${v !== null && v >= 5 ? 'text-[#1E875F]' : v !== null ? 'text-[#D7372C]' : 'text-text-muted'}`}>
        {v !== null ? v.toFixed(1) : 'N/A'}
      </span>
    ),
  },
];

export default function CourseStatisticsPage() {
  const { data, isLoading } = useCourseStatistics();
  const navigate = useNavigate();

  if (isLoading) {
    return <div className="flex justify-center pt-20"><Spin size="large" /></div>;
  }

  const items = data?.items ?? [];
  const courseNames = items.map((c) => c.courseName);

  // ECharts: Enrolled per course (bar chart, primary color)
  const enrolledOption = {
    tooltip: { trigger: 'axis' as const },
    xAxis: { type: 'category' as const, data: courseNames, axisLabel: { rotate: 30, fontSize: 11 } },
    yAxis: { type: 'value' as const },
    series: [{ data: items.map((c) => c.enrolledCount), type: 'bar' as const, itemStyle: { color: '#F45D48', borderRadius: [6, 6, 0, 0] } }],
    grid: { left: 40, right: 16, bottom: 60, top: 16 },
  };

  // ECharts: Avg score per course (bar chart, green)
  const avgScoreOption = {
    tooltip: { trigger: 'axis' as const },
    xAxis: { type: 'category' as const, data: courseNames, axisLabel: { rotate: 30, fontSize: 11 } },
    yAxis: { type: 'value' as const, max: 10 },
    series: [{ data: items.map((c) => c.averageScore ?? 0), type: 'bar' as const, itemStyle: { color: '#1E875F', borderRadius: [6, 6, 0, 0] } }],
    grid: { left: 40, right: 16, bottom: 60, top: 16 },
  };

  return (
    <div className="p-10 px-12">
      <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Course statistics</h1>
      <p className="m-0 mb-7 text-[15px] text-text-secondary">
        Enrollment and grade overview across all courses.
      </p>

      {/* Charts */}
      <div className="mb-7 grid grid-cols-2 gap-5">
        <div className="rounded-xl border border-border bg-white p-6 shadow-sm">
          <div className="mb-1 text-[15px] font-semibold">Students enrolled per course</div>
          <div className="mb-6 text-[13px] text-text-muted">Each column is one course</div>
          <ReactECharts option={enrolledOption} style={{ height: 220 }} />
        </div>
        <div className="rounded-xl border border-border bg-white p-6 shadow-sm">
          <div className="mb-1 text-[15px] font-semibold">Average score per course</div>
          <div className="mb-6 text-[13px] text-text-muted">Each column is one course (0–10)</div>
          <ReactECharts option={avgScoreOption} style={{ height: 220 }} />
        </div>
      </div>

      {/* Table */}
      <Table<CourseStatisticsItem>
        columns={columns}
        dataSource={items}
        rowKey="courseId"
        pagination={false}
        onRow={(record) => ({
          onClick: () => navigate(`/admin/reports/courses/${record.courseId}`),
          style: { cursor: 'pointer' },
        })}
      />
    </div>
  );
}
