import { useState } from 'react';
import { Input, InputNumber, Table, Button } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import { getGradeBand } from '@/shared/lib/grade-band';
import type { CourseStatus } from '@/modules/course-management/shared/course.types';
import { useGrading } from './useGrading';
import type { EnrollmentItem } from './grading.types';

// Nội dung tab Grading trong CourseDetailPage — nhập/sửa điểm cho từng Student.
// Chỉ sửa được khi Course Active/Completed (khớp rule backend).
export default function GradingPanel({
  courseId,
  courseStatus,
}: {
  courseId: string;
  courseStatus?: CourseStatus;
}) {
  const canEditGrade = courseStatus === 'Active' || courseStatus === 'Completed';
  const grading = useGrading(courseId);

  // Giá trị đang gõ dở (draft) cho từng dòng, chưa lưu xuống server
  const [drafts, setDrafts] = useState<Record<string, number | null>>({});

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

  return (
    <div className="max-w-[920px]">
      <p className="m-0 mb-4 text-[14px] text-text-muted">
        Grades range 0–10. Saving a grade emails the student automatically.
      </p>

      {!canEditGrade && (
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
          className="w-full sm:max-w-[360px]"
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
