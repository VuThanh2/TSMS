import { useParams, useNavigate } from 'react-router-dom';
import { Button, Table, Tabs, Spin } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import ReactECharts from 'echarts-for-react';
import type { ColumnsType } from 'antd/es/table';

import type { StudentGradeItem, AttendanceReportItem } from '@/modules/reporting/shared/reporting.types';
import { useCourseReport, type ReportTab } from './useCourseReport';

const PIE_COLORS = ['#F45D48', '#E5A20B', '#2E73C4', '#1E875F', '#8A847E'];

const gradeColumns: ColumnsType<StudentGradeItem> = [
  { title: 'Student', dataIndex: 'studentFullName', key: 'name', render: (v: string) => <span className="font-semibold">{v}</span> },
  { title: 'Email', dataIndex: 'studentEmail', key: 'email', render: (v: string) => <span className="font-mono text-[14px] text-text-secondary">{v}</span> },
  {
    title: 'Score', dataIndex: 'grade', key: 'grade', align: 'right',
    render: (v: number | null) => (
      <span className={`font-mono text-[15px] font-semibold ${v !== null && v >= 5 ? 'text-[#1E875F]' : v !== null ? 'text-[#D7372C]' : 'text-text-muted'}`}>
        {v !== null ? v.toFixed(1) : 'Not graded'}
      </span>
    ),
  },
];

const attendanceColumns: ColumnsType<AttendanceReportItem> = [
  { title: 'Student', dataIndex: 'studentFullName', key: 'name', render: (v: string) => <span className="font-semibold">{v}</span> },
  { title: 'Present', dataIndex: 'presentCount', key: 'present', align: 'center', render: (v: number) => <span className="font-mono font-semibold text-[#1E875F]">{v}</span> },
  { title: 'Excused', dataIndex: 'excusedCount', key: 'excused', align: 'center', render: (v: number) => <span className="font-mono font-semibold text-[#E5A20B]">{v}</span> },
  { title: 'Absent', dataIndex: 'absentCount', key: 'absent', align: 'center', render: (v: number) => <span className="font-mono font-semibold text-[#D7372C]">{v}</span> },
  {
    title: 'Rate', dataIndex: 'attendanceRate', key: 'rate', align: 'right',
    render: (v: number) => <span className="font-mono text-[14px] font-semibold">{(v * 100).toFixed(0)}%</span>,
  },
];

export default function CourseReportPage() {
  const { courseId } = useParams<{ courseId: string }>();
  const navigate = useNavigate();
  const report = useCourseReport(courseId!);

  const courseName =
    report.grades.data?.courseName ??
    report.attendance.data?.courseName ??
    report.distribution.data?.courseName ??
    'Course Report';

  // Pie chart options
  const distItems = report.distribution.data?.items ?? [];
  const pieOption = distItems.length > 0
    ? {
        tooltip: { trigger: 'item' as const },
        series: [{
          type: 'pie' as const,
          radius: ['45%', '75%'],
          data: distItems.map((d, i) => ({
            name: d.scoreGroup,
            value: d.studentCount,
            itemStyle: { color: PIE_COLORS[i % PIE_COLORS.length] },
          })),
          label: { show: false },
        }],
      }
    : null;

  return (
    <div className="max-w-[1000px] p-10 px-12">
      <Button
        type="text"
        icon={<ArrowLeftOutlined />}
        onClick={() => navigate('/admin/reports/statistics')}
        className="mb-4 p-0 text-text-secondary"
      >
        Back to statistics
      </Button>

      <h1 className="m-0 mb-5 text-[28px] font-bold tracking-tight">{courseName}</h1>

      <Tabs
        activeKey={report.activeTab}
        onChange={(key) => report.setActiveTab(key as ReportTab)}
        items={[
          {
            key: 'grades',
            label: 'Student grades',
            children: report.grades.isLoading ? (
              <div className="flex justify-center py-8"><Spin /></div>
            ) : (
              <Table<StudentGradeItem>
                columns={gradeColumns}
                dataSource={report.grades.data?.items ?? []}
                rowKey="enrollmentId"
                pagination={false}
              />
            ),
          },
          {
            key: 'attendance',
            label: 'Attendance',
            children: report.attendance.isLoading ? (
              <div className="flex justify-center py-8"><Spin /></div>
            ) : (
              <Table<AttendanceReportItem>
                columns={attendanceColumns}
                dataSource={report.attendance.data?.items ?? []}
                rowKey="enrollmentId"
                pagination={false}
              />
            ),
          },
          {
            key: 'distribution',
            label: 'Score distribution',
            children: report.distribution.isLoading ? (
              <div className="flex justify-center py-8"><Spin /></div>
            ) : distItems.length === 0 ? (
              <div className="rounded-xl border border-border bg-white p-14 text-center text-[15px] text-text-muted">
                Chưa có dữ liệu — chưa có sinh viên nào được chấm điểm.
              </div>
            ) : (
              <div className="flex items-center gap-12 rounded-xl border border-border bg-white p-8 shadow-sm">
                <div className="w-[200px] flex-none">
                  <ReactECharts option={pieOption!} style={{ height: 200, width: 200 }} />
                  <div className="mt-2 text-center">
                    <span className="text-[12px] text-text-muted">Graded</span>
                    <div className="font-mono text-[24px] font-bold">
                      {report.distribution.data?.gradedStudentCount ?? 0}
                    </div>
                  </div>
                </div>
                <div className="flex flex-1 flex-col gap-3.5">
                  {distItems.map((d, i) => (
                    <div key={d.scoreGroup} className="flex items-center gap-3">
                      <span
                        className="h-3.5 w-3.5 flex-none rounded"
                        style={{ background: PIE_COLORS[i % PIE_COLORS.length] }}
                      />
                      <span className="flex-1 text-[15px] font-semibold">{d.scoreGroup}</span>
                      <span className="text-[13px] text-text-muted">
                        {d.rangeStart}–{d.rangeEnd}
                      </span>
                      <span className="w-16 text-right font-mono text-[15px] font-semibold">
                        {d.studentCount} ({d.percentage.toFixed(0)}%)
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ),
          },
        ]}
      />
    </div>
  );
}
