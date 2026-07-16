import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Table, Input, Button } from 'antd';
import { PlusOutlined, SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import StatusTag from '@/shared/components/StatusTag';
import type { CourseListItem } from '@/modules/course-management/shared/course.types';
import CreateCourseModal from '@/modules/course-management/create-course/CreateCourseModal';
import { useCourseGrid } from './useCourseGrid';

const STATUS_OPTIONS = ['', 'Upcoming', 'Active', 'Completed'] as const;
const STATUS_LABELS: Record<string, string> = { '': 'All', Upcoming: 'Upcoming', Active: 'Active', Completed: 'Completed' };

// Lưới phân trang server-side → sorter: true, để SQL sort trên toàn bộ tập kết quả.
// Lecturer và Capacity CỐ Ý không có sorter: lecturerName/enrolledCount do BC khác sở hữu
// và chỉ được enrich sau khi phân trang, nên SQL không nhìn thấy chúng lúc ORDER BY —
// muốn sort được thì phải JOIN cross-BC, điều mà kiến trúc cấm.
const columns: ColumnsType<CourseListItem> = [
  {
    title: 'Course',
    dataIndex: 'name',
    key: 'name',
    sorter: true,
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
    key: 'lecturerName',
    render: (name: string) => <span className="text-text-secondary">{name ?? '—'}</span>,
  },
  {
    title: 'Status',
    dataIndex: 'status',
    key: 'status',
    // Status map HasConversion<string> dưới DB → sort theo bảng chữ cái
    // (Active → Completed → Upcoming), không theo thứ tự vòng đời.
    sorter: true,
    render: (status: CourseListItem['status']) => <StatusTag status={status} />,
  },
  {
    title: 'Capacity',
    key: 'capacity',
    render: (_, record) => (
      <span className="font-mono text-[14px] font-medium">
        {record.enrolledCount}/{record.maxCapacity}
      </span>
    ),
  },
];

export default function CourseGridPage() {
  const navigate = useNavigate();
  const grid = useCourseGrid();
  const [createOpen, setCreateOpen] = useState(false);

  return (
    <div className="p-5 sm:p-8 md:p-10 md:px-12">
      {/* Header */}
      <div className="mb-7 flex items-start justify-between gap-4">
        <div>
          <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Courses</h1>
          <p className="m-0 text-[15px] text-text-secondary">
            {grid.totalCount} course{grid.totalCount !== 1 ? 's' : ''} total
          </p>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          size="large"
          className="h-11"
          onClick={() => setCreateOpen(true)}
        >
          Create course
        </Button>
      </div>

      {/* Filters */}
      <div className="mb-5 flex flex-wrap gap-3">
        <Input
          placeholder="Search by course name…"
          prefix={<SearchOutlined />}
          value={grid.keyword}
          onChange={(e) => {
            grid.setKeyword(e.target.value);
            grid.setPage(1);
          }}
          allowClear
          className="h-11 min-w-[240px] flex-1"
          size="large"
        />
        <div className="flex gap-1.5 rounded-lg border border-border bg-white p-1">
          {STATUS_OPTIONS.map((s) => (
            <button
              key={s}
              onClick={() => {
                grid.setStatus(s);
                grid.setPage(1);
              }}
              className={`cursor-pointer rounded-md border-none px-3 py-1.5 text-[13px] font-semibold transition-colors ${
                grid.status === s
                  ? 'bg-primary text-white'
                  : 'bg-transparent text-text-muted hover:text-text'
              }`}
            >
              {STATUS_LABELS[s]}
            </button>
          ))}
        </div>
      </div>

      {/* Table */}
      <Table<CourseListItem>
        columns={columns}
        dataSource={grid.courses}
        rowKey="courseId"
        loading={grid.isLoading}
        locale={{ emptyText: 'No courses match your search.' }}
        rowClassName="tsms-clickable-row"
        pagination={{
          current: grid.page,
          pageSize: grid.pageSize,
          total: grid.totalCount,
          showSizeChanger: true,
          onChange: (p, ps) => {
            grid.setPage(p);
            grid.setPageSize(ps);
          },
        }}
        // Lọc theo extra.action: onChange fire cho cả phân trang lẫn sort, không lọc thì
        // setPage(1) sẽ ghi đè pagination.onChange và làm hỏng việc chuyển trang.
        onChange={(_pagination, _filters, sorter, extra) => {
          if (extra.action !== 'sort') return;
          grid.applySorter(sorter);
          grid.setPage(1);
        }}
        onRow={(record) => ({
          onClick: () => navigate(`/admin/courses/${record.courseId}`),
          style: { cursor: 'pointer' },
        })}
      />

      <CreateCourseModal open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}
