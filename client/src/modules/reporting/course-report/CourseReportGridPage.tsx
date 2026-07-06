import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Table, Input } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';

import { useAuth } from '@/shared/lib/auth-context';
import StatusTag from '@/shared/components/StatusTag';
import type { CourseListItem } from '@/modules/course-management/shared/course.types';
import { getCoursesApi, getMyCourseApi } from '@/modules/course-management/course-grid/course-grid.api';

const columns: ColumnsType<CourseListItem> = [
  {
    title: 'Course',
    dataIndex: 'name',
    key: 'name',
    render: (name: string, record) => (
      <div>
        <div className="text-[15px] font-semibold">{name}</div>
        <div className="text-[13px] text-text-muted">
          {record.startDate} — {record.endDate}
        </div>
      </div>
    ),
  },
  {
    title: 'Lecturer',
    dataIndex: 'lecturerName',
    key: 'lecturer',
    render: (v: string) => <span className="text-text-secondary">{v}</span>,
  },
  {
    title: 'Status',
    dataIndex: 'status',
    key: 'status',
    render: (status: CourseListItem['status']) => <StatusTag status={status} />,
  },
  {
    title: 'Enrolled',
    key: 'capacity',
    align: 'right',
    render: (_, record) => (
      <span className="font-mono text-[14px]">
        {record.enrolledCount}/{record.maxCapacity}
      </span>
    ),
  },
];

export default function CourseReportGridPage() {
  const navigate = useNavigate();
  const { state: authState } = useAuth();
  const isAdmin = authState.status === 'authenticated' && authState.user.role === 'Admin';

  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const params = { keyword: keyword || undefined, page, pageSize };

  const { data, isLoading } = useQuery({
    queryKey: ['report-courses', isAdmin ? 'admin' : 'lecturer', params],
    queryFn: () => (isAdmin ? getCoursesApi(params) : getMyCourseApi(params)),
    select: (res) => res.data,
  });

  const courses = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  function handleRowClick(courseId: string) {
    const basePath = isAdmin ? '/admin/reports/courses' : '/lecturer/reports/courses';
    navigate(`${basePath}/${courseId}`);
  }

  return (
    <div className="p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Course Reports</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Chọn một khóa học để xem báo cáo chi tiết.
        </p>
      </div>

      <div className="mb-5">
        <Input
          placeholder="Search by course name…"
          prefix={<SearchOutlined />}
          value={keyword}
          onChange={(e) => {
            setKeyword(e.target.value);
            setPage(1);
          }}
          allowClear
          size="large"
          className="max-w-[360px]"
        />
      </div>

      <Table<CourseListItem>
        columns={columns}
        dataSource={courses}
        rowKey="courseId"
        loading={isLoading}
        pagination={{
          current: page,
          pageSize,
          total: totalCount,
          showSizeChanger: true,
          onChange: (p, ps) => {
            setPage(p);
            setPageSize(ps);
          },
        }}
        onRow={(record) => ({
          onClick: () => handleRowClick(record.courseId),
          style: { cursor: 'pointer' },
        })}
      />
    </div>
  );
}
