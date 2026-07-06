import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Input, InputNumber, Table, Select, Button, Space, Empty } from 'antd';
import { SearchOutlined, CheckOutlined, CloseOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import StatusTag from '@/shared/components/StatusTag';
import { useLecturerCourses } from '@/modules/enrollment-management/shared/useLecturerCourses';
import { useGrading } from './useGrading';
import type { EnrollmentItem } from './grading.types';

const GRADE_COLOR = (g: number | null) => {
  if (g === null) return 'text-text-muted';
  return g >= 5 ? 'text-[#1E875F]' : 'text-[#D7372C]';
};

export default function GradingPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const courseId = searchParams.get('courseId') ?? '';

  const { courses, isLoading: coursesLoading } = useLecturerCourses();
  const selectedCourse = courses.find((c) => c.courseId === courseId);
  const canEditGrade =
    selectedCourse?.status === 'Active' || selectedCourse?.status === 'Completed';

  const grading = useGrading(courseId);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editValue, setEditValue] = useState<number | null>(null);

  function selectCourse(id: string) {
    setSearchParams(id ? { courseId: id } : {});
    setEditingId(null);
    setEditValue(null);
    grading.setKeyword('');
    grading.setPage(1);
  }

  function startEdit(enrollment: EnrollmentItem) {
    setEditingId(enrollment.enrollmentId);
    setEditValue(enrollment.grade);
  }

  function cancelEdit() {
    setEditingId(null);
    setEditValue(null);
  }

  function saveGrade(enrollmentId: string) {
    if (editValue === null || editValue === undefined) return;
    grading.updateGrade.mutate(
      { enrollmentId, grade: editValue },
      { onSuccess: () => { setEditingId(null); setEditValue(null); } },
    );
  }

  const columns: ColumnsType<EnrollmentItem> = [
    {
      title: 'Sinh viên',
      dataIndex: 'studentFullName',
      key: 'name',
      render: (v: string) => <span className="font-semibold">{v}</span>,
    },
    {
      title: 'Email',
      dataIndex: 'studentEmail',
      key: 'email',
      render: (v: string) => (
        <span className="font-mono text-[14px] text-text-secondary">{v}</span>
      ),
    },
    {
      title: 'Điểm (0–10)',
      key: 'grade',
      align: 'right',
      render: (_, record) => {
        if (editingId === record.enrollmentId) {
          return (
            <Space size={4}>
              <InputNumber
                min={0}
                max={10}
                step={0.1}
                precision={1}
                value={editValue}
                onChange={(v) => setEditValue(v)}
                style={{ width: 80 }}
                autoFocus
              />
              <Button
                type="primary"
                size="small"
                icon={<CheckOutlined />}
                loading={grading.updateGrade.isPending}
                onClick={() => saveGrade(record.enrollmentId)}
              />
              <Button size="small" icon={<CloseOutlined />} onClick={cancelEdit} />
            </Space>
          );
        }
        return (
          <Space size={8}>
            <span className={`font-mono text-[15px] font-semibold ${GRADE_COLOR(record.grade)}`}>
              {record.grade !== null ? record.grade.toFixed(1) : 'Chưa có điểm'}
            </span>
            {canEditGrade && (
              <Button size="small" onClick={() => startEdit(record)} disabled={editingId !== null}>
                Sửa
              </Button>
            )}
          </Space>
        );
      },
    },
  ];

  return (
    <div className="max-w-[1040px] p-10 px-12">
      <div className="mb-7">
        <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Grading</h1>
        <p className="m-0 text-[15px] text-text-secondary">
          Chọn một khóa học bạn phụ trách để nhập/sửa điểm cho sinh viên.
        </p>
      </div>

      {/* Course selector */}
      <div className="mb-6 max-w-[420px]">
        <label className="mb-1.5 block text-[13px] font-semibold text-text-muted">Khóa học</label>
        <Select
          value={courseId || undefined}
          onChange={selectCourse}
          loading={coursesLoading}
          placeholder="Chọn khóa học…"
          size="large"
          className="w-full"
          showSearch
          options={courses.map((c) => ({ value: c.courseId, label: c.name }))}
        />
      </div>

      {!courseId ? (
        <Empty description="Chọn một khóa học để xem danh sách sinh viên và điểm." className="py-16" />
      ) : (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-3">
            <h2 className="m-0 text-[20px] font-semibold tracking-tight">{selectedCourse?.name}</h2>
            {selectedCourse && <StatusTag status={selectedCourse.status} />}
          </div>

          {!canEditGrade && selectedCourse && (
            <div className="mb-4 rounded-lg border border-border bg-bg-card px-4 py-3 text-[14px] text-text-secondary">
              Chỉ có thể nhập điểm khi khóa học đang <strong>Active</strong> hoặc{' '}
              <strong>Completed</strong>.
            </div>
          )}

          <div className="mb-4">
            <Input
              placeholder="Tìm theo tên hoặc email sinh viên…"
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
            columns={columns}
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
        </>
      )}
    </div>
  );
}
