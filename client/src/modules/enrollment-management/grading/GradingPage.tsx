import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Input, InputNumber, Table, Button, Empty, Spin } from 'antd';
import { ArrowLeftOutlined, SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import StatusTag from '@/shared/components/StatusTag';
import { getGradeBand } from '@/shared/lib/grade-band';
import { useLecturerCourses } from '@/modules/enrollment-management/shared/useLecturerCourses';
import type { CourseListItem } from '@/modules/course-management/shared/course.types';
import { useGrading } from './useGrading';
import type { EnrollmentItem } from './grading.types';

// Ghi chú: API Contract Mapping không có endpoint riêng cho "Grading list" kèm avg score
// — màn hình chọn khóa học tái dùng GET /api/courses/my-courses (giống useLecturerCourses).
// Vì vậy cột "Avg score" hiện "—" thay vì suy đoán dữ liệu không có sẵn.
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
    title: 'Students',
    key: 'students',
    align: 'right',
    render: (_, record) => (
      <span className="font-mono text-[14px] font-medium">
        {record.enrolledCount}/{record.maxCapacity}
      </span>
    ),
  },
  {
    title: 'Avg score',
    key: 'avg',
    align: 'right',
    render: () => <span className="font-mono text-[14px] text-text-muted">—</span>,
  },
];

export default function GradingPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const courseId = searchParams.get('courseId') ?? '';

  const { courses, isLoading: coursesLoading } = useLecturerCourses();
  const selectedCourse = courses.find((c) => c.courseId === courseId);
  const canEditGrade =
    selectedCourse?.status === 'Active' || selectedCourse?.status === 'Completed';

  const grading = useGrading(courseId);

  // Giá trị đang gõ dở (draft) cho từng dòng, chưa lưu xuống server
  const [drafts, setDrafts] = useState<Record<string, number | null>>({});

  function openCourse(id: string) {
    setSearchParams({ courseId: id });
  }

  function backToList() {
    setSearchParams({});
    setDrafts({});
  }

  function saveGrade(enrollmentId: string, value: number | null) {
    if (value === null) return;
    grading.updateGrade.mutate(
      { enrollmentId, grade: value },
      {
        onSuccess: () => {
          setDrafts((prev) => {
            const next = { ...prev };
            delete next[enrollmentId];
            return next;
          });
        },
      },
    );
  }

  const rosterColumns: ColumnsType<EnrollmentItem> = [
    {
      title: 'Student',
      dataIndex: 'studentFullName',
      key: 'name',
      render: (v: string) => <span className="text-[15px] font-semibold">{v}</span>,
    },
    {
      title: 'Email',
      dataIndex: 'studentEmail',
      key: 'email',
      render: (v: string) => <span className="font-mono text-[14px] text-text-secondary">{v}</span>,
    },
    {
      title: 'Grade',
      key: 'grade',
      align: 'right',
      render: (_, record) => {
        const hasDraft = Object.prototype.hasOwnProperty.call(drafts, record.enrollmentId);
        const value = hasDraft ? drafts[record.enrollmentId] : record.grade;
        const isDraft = hasDraft && drafts[record.enrollmentId] !== record.grade;
        const dotColor = isDraft
          ? '#E5A20B'
          : value !== null
            ? getGradeBand(value).color
            : 'var(--color-border-input)';
        return (
          <div className="flex items-center justify-end gap-2.5">
            <span className="h-2 w-2 flex-none rounded-full" style={{ backgroundColor: dotColor }} />
            <InputNumber
              min={0}
              max={10}
              step={0.1}
              precision={1}
              value={value}
              disabled={!canEditGrade}
              onChange={(v) => setDrafts((prev) => ({ ...prev, [record.enrollmentId]: v }))}
              style={{ width: 84 }}
            />
            <Button
              type="primary"
              size="small"
              disabled={!canEditGrade || !isDraft}
              loading={grading.updateGrade.isPending}
              onClick={() => saveGrade(record.enrollmentId, value)}
            >
              Save
            </Button>
          </div>
        );
      },
    },
  ];

  if (!courseId) {
    return (
      <div className="p-10 px-12">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Grading</h1>
        <p className="m-0 mb-7 text-[15px] text-text-secondary">
          Select a course to enter or update student grades.
        </p>

        {coursesLoading ? (
          <div className="flex justify-center pt-16"><Spin size="large" /></div>
        ) : courses.length === 0 ? (
          <Empty description="You aren't assigned to any courses yet." className="py-16" />
        ) : (
          <Table<CourseListItem>
            columns={courseListColumns}
            dataSource={courses}
            rowKey="courseId"
            pagination={false}
            onRow={(record) => ({
              onClick: () => openCourse(record.courseId),
              style: { cursor: 'pointer' },
            })}
          />
        )}
      </div>
    );
  }

  return (
    <div className="max-w-[920px] p-10 px-12">
      <Button type="text" icon={<ArrowLeftOutlined />} onClick={backToList} className="mb-4 p-0 text-text-secondary">
        Back to grading
      </Button>

      <div className="mb-1.5 flex flex-wrap items-center gap-3">
        <h1 className="m-0 text-[28px] font-bold tracking-tight">{selectedCourse?.name}</h1>
        {selectedCourse && <StatusTag status={selectedCourse.status} />}
      </div>
      <p className="m-0 mb-[22px] text-[14px] text-text-muted">
        Grades range 0–10. Saving a grade emails the student automatically.
      </p>

      {!canEditGrade && selectedCourse && (
        <div className="mb-4 rounded-lg border border-border bg-bg-card px-4 py-3 text-[14px] text-text-secondary">
          Grades can only be entered while the course is <strong>Active</strong> or <strong>Completed</strong>.
        </div>
      )}

      <div className="mb-5">
        <Input
          placeholder="Search student by name or email…"
          prefix={<SearchOutlined />}
          value={grading.keyword}
          onChange={(e) => {
            grading.setKeyword(e.target.value);
            grading.setPage(1);
          }}
          allowClear
          size="large"
          className="max-w-[360px]"
        />
      </div>

      <Table<EnrollmentItem>
        columns={rosterColumns}
        dataSource={grading.enrollments}
        rowKey="enrollmentId"
        loading={grading.isLoading}
        pagination={{
          current: grading.page,
          pageSize: grading.pageSize,
          total: grading.totalCount,
          showSizeChanger: true,
          onChange: (p, ps) => {
            grading.setPage(p);
            grading.setPageSize(ps);
          },
        }}
      />
    </div>
  );
}
