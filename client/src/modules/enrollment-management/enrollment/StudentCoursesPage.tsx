import { useState } from 'react';
import { Table, Button } from 'antd';
import { useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';

import StatusTag from '@/shared/components/StatusTag';
import { useGradeHub } from '@/shared/hooks/useGradeHub';
import { useMyEnrollments } from './useMyEnrollments';
import AdjustSessionModal from './AdjustSessionModal';
import type { MyCourseItem } from './enrollment.types';

const GRADE_COLOR = (g: number | null) => {
  if (g === null) return 'text-text-muted';
  return g >= 5 ? 'text-[#1E875F]' : 'text-[#D7372C]';
};

const columns = (
  onAdjust: (course: MyCourseItem) => void,
): ColumnsType<MyCourseItem> => [
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
    render: (v: MyCourseItem['status']) => <StatusTag status={v} />,
  },
  {
    title: 'Grade',
    dataIndex: 'grade',
    key: 'grade',
    align: 'right',
    render: (v: number | null) =>
      v !== null ? (
        <span className={`font-mono text-[15px] font-semibold ${GRADE_COLOR(v)}`}>
          {v.toFixed(1)}
        </span>
      ) : (
        <span className="text-[13px] text-text-muted">Not graded yet</span>
      ),
  },
  {
    title: '',
    key: 'action',
    align: 'right',
    render: (_, record) => {
      if (record.status === 'Completed') return null;
      return (
        <Button size="small" onClick={() => onAdjust(record)}>
          Adjust session
        </Button>
      );
    },
  },
];

export default function StudentCoursesPage() {
  const queryClient = useQueryClient();
  const enroll = useMyEnrollments();
  const [adjustTarget, setAdjustTarget] = useState<MyCourseItem | null>(null);

  // UC-33: cập nhật điểm real-time khi Lecturer chấm
  useGradeHub(() => {
    void queryClient.invalidateQueries({ queryKey: ['my-course-enrollments'] });
  });

  return (
    <div className="p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">My courses</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Courses you&apos;re enrolled in, with status and grades.
        </p>
      </div>

      <Table<MyCourseItem>
        columns={columns((course) => setAdjustTarget(course))}
        dataSource={enroll.myCourses}
        rowKey="enrollmentId"
        loading={enroll.isLoading}
        pagination={{
          current: enroll.page,
          pageSize: enroll.pageSize,
          total: enroll.totalCount,
          showSizeChanger: true,
          onChange: (p, ps) => {
            enroll.setPage(p);
            enroll.setPageSize(ps);
          },
        }}
      />

      <AdjustSessionModal
        enrollment={adjustTarget}
        onClose={() => setAdjustTarget(null)}
        isLoading={enroll.adjustSessionMutation.isPending}
        onConfirm={(enrollmentId, oldWeeklySlotId, newWeeklySlotId) => {
          enroll.adjustSessionMutation.mutate(
            { enrollmentId, oldWeeklySlotId, newWeeklySlotId },
            { onSuccess: () => setAdjustTarget(null) },
          );
        }}
      />
    </div>
  );
}
