import { useState } from 'react';
import { Table, Button, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';

import { useAvailableCourses } from './useAvailableCourses';
import EnrollModal from './EnrollModal';
import type { AvailableCourse } from './enrollment.types';

const columns = (
  onEnroll: (course: AvailableCourse) => void,
): ColumnsType<AvailableCourse> => [
  {
    title: 'Khóa học',
    dataIndex: 'name',
    key: 'name',
    render: (name: string, record) => (
      <div>
        <div className="font-semibold">{name}</div>
        <div className="text-[13px] text-text-muted">
          {record.startDate} — {record.endDate}
        </div>
      </div>
    ),
  },
  {
    title: 'Giảng viên',
    dataIndex: 'lecturerName',
    key: 'lecturer',
    render: (v: string) => <span className="text-text-secondary">{v}</span>,
  },
  {
    title: 'Còn chỗ',
    key: 'capacity',
    align: 'center',
    render: (_, record) => {
      const remaining = record.maxCapacity - record.enrolledCount;
      return (
        <Tag color={remaining > 0 ? 'green' : 'red'}>
          {remaining > 0 ? `${remaining} chỗ còn` : 'Hết chỗ'}
        </Tag>
      );
    },
  },
  {
    title: '',
    key: 'action',
    align: 'right',
    render: (_, record) => (
      <Button
        type="primary"
        size="small"
        disabled={record.enrolledCount >= record.maxCapacity}
        onClick={() => onEnroll(record)}
      >
        Đăng ký
      </Button>
    ),
  },
];

export default function AvailableCoursesPage() {
  const available = useAvailableCourses();
  const [enrollTarget, setEnrollTarget] = useState<AvailableCourse | null>(null);

  return (
    <div className="p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Available Courses</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Các khóa học đang mở đăng ký. Chọn đúng 2 slot để hoàn tất.
        </p>
      </div>

      <Table<AvailableCourse>
        columns={columns((course) => setEnrollTarget(course))}
        dataSource={available.courses}
        rowKey="courseId"
        loading={available.isLoading}
        pagination={{
          current: available.page,
          pageSize: available.pageSize,
          total: available.totalCount,
          showSizeChanger: true,
          onChange: (p, ps) => {
            available.setPage(p);
            available.setPageSize(ps);
          },
        }}
      />

      <EnrollModal
        course={enrollTarget}
        onClose={() => setEnrollTarget(null)}
        isLoading={available.enrollMutation.isPending}
        onConfirm={(courseId, weeklySlotIds) => {
          available.enrollMutation.mutate(
            { courseId, weeklySlotIds },
            { onSuccess: () => setEnrollTarget(null) },
          );
        }}
      />
    </div>
  );
}
