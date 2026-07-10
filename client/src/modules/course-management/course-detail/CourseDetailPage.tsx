import { Fragment, useState } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { Button, Modal, Form, Input, InputNumber, DatePicker, Spin, Popconfirm, Switch, Table, Tabs, Tag } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';

import StatusTag from '@/shared/components/StatusTag';
import LecturerPicker from '@/modules/course-management/shared/LecturerPicker';
import CourseWeeklySchedule from './CourseWeeklySchedule';
import AttendancePanel from '@/modules/enrollment-management/attendance/AttendancePanel';
import GradingPanel from '@/modules/enrollment-management/grading/GradingPanel';
import type { ClassSession, WeeklySlot } from '@/modules/course-management/shared/course.types';
import { useAuth, getDefaultRouteForRole } from '@/shared/lib/auth-context';
import {
  useCourseDetail,
  useUpdateCourse,
  useReplaceLecturer,
  useAddWeeklySlot,
  useRemoveWeeklySlot,
  useDeleteCourse,
} from './useCourseDetail';

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
const DAY_SHORT = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const SESSION_TYPES = ['Morning', 'Afternoon'];

// Lưới weekly pattern chỉ dùng cho Admin (thêm/xóa slot). Lecturer xem lịch thật
// qua CourseWeeklySchedule nên grid này không còn cần chế độ read-only.
function WeeklySlotGrid({
  slots,
  onAdd,
  onRemove,
}: {
  slots: WeeklySlot[];
  onAdd: (dayOfWeek: string, sessionType: string) => void;
  onRemove: (slot: WeeklySlot) => void;
}) {
  function findSlot(day: string, type: string) {
    return slots.find((s) => s.dayOfWeek === day && s.sessionType === type);
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-border bg-white p-4 shadow-sm">
      <div className="grid items-center gap-2" style={{ gridTemplateColumns: '56px repeat(7, 1fr)', minWidth: 540 }}>
        <div />
        {DAY_SHORT.map((d) => (
          <div key={d} className="text-center text-[12px] font-semibold uppercase tracking-wide text-text-muted">
            {d}
          </div>
        ))}
        {SESSION_TYPES.map((type) => (
          <Fragment key={type}>
            <div className="text-[12px] font-bold text-text-muted">
              {type === 'Morning' ? 'AM' : 'PM'}
            </div>
            {DAYS.map((day) => {
              const slot = findSlot(day, type);
              return slot ? (
                <Popconfirm
                  key={`${day}-${type}`}
                  title="Remove this weekly slot?"
                  description="All related sessions will be deleted."
                  onConfirm={() => onRemove(slot)}
                  okText="Remove"
                  okButtonProps={{ danger: true }}
                >
                  <button className="flex h-10 cursor-pointer items-center justify-center rounded-lg border-none bg-primary text-[13px] font-semibold text-white transition-opacity hover:opacity-80">
                    ✓
                  </button>
                </Popconfirm>
              ) : (
                <button
                  key={`${day}-${type}`}
                  onClick={() => onAdd(day, type)}
                  className="flex h-10 cursor-pointer items-center justify-center rounded-lg border border-dashed border-border-input bg-transparent text-[16px] text-text-muted transition-colors hover:border-primary hover:text-primary"
                >
                  +
                </button>
              );
            })}
          </Fragment>
        ))}
      </div>
    </div>
  );
}

const sessionColumns: ColumnsType<ClassSession> = [
  {
    title: 'Date',
    dataIndex: 'sessionDate',
    key: 'sessionDate',
    render: (date: string) => <span className="font-mono text-[14px] font-medium">{date}</span>,
  },
  {
    title: 'Day',
    dataIndex: 'dayOfWeek',
    key: 'dayOfWeek',
  },
  {
    title: 'Session',
    dataIndex: 'sessionType',
    key: 'sessionType',
    render: (type: string) => <span>{type === 'Morning' ? 'AM' : 'PM'}</span>,
  },
  {
    title: 'Status',
    key: 'status',
    render: (_, record) => {
      if (record.isCancelled) return <Tag color="red">Cancelled</Tag>;
      if (record.isPast) return <Tag color="default">Past</Tag>;
      return <Tag color="green">Upcoming</Tag>;
    },
  },
];

export default function CourseDetailPage() {
  const { courseId } = useParams<{ courseId: string }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { state } = useAuth();
  const { course, weeklySlots } = useCourseDetail(courseId!);

  const [editOpen, setEditOpen] = useState(false);
  const [replaceOpen, setReplaceOpen] = useState(false);
  const [hidePast, setHidePast] = useState(false);
  const [activeTab, setActiveTab] = useState(searchParams.get('tab') ?? 'detail');
  const [editForm] = Form.useForm();
  const [replaceForm] = Form.useForm();

  const updateCourse = useUpdateCourse(courseId!, () => setEditOpen(false));
  const replaceLecturer = useReplaceLecturer(courseId!, () => setReplaceOpen(false));
  const addSlot = useAddWeeklySlot(courseId!);
  const removeSlot = useRemoveWeeklySlot(courseId!);
  const deleteCourse = useDeleteCourse(courseId!);

  if (course.isLoading) {
    return <div className="flex justify-center pt-20"><Spin size="large" /></div>;
  }

  const c = course.data;
  if (!c) return null;

  // Edit/Replace lecturer/Add-remove WeeklySlot chỉ Admin được làm (UC-14/15/16/18
  // trong docs/Screen Inventory.md) — Lecturer/Student xem cùng màn hình ở chế độ read-only.
  const role = state.status === 'authenticated' ? state.user.role : undefined;
  const canManage = role === 'Admin';
  const backTo = role ? getDefaultRouteForRole(role) : '/admin/dashboard';

  // Deep-link: Schedule bấm 1 buổi → mở CourseDetail ở tab Attendance với buổi đã chọn.
  // (activeTab dùng useState khai báo cùng các hook khác — PHẢI trước early return.)
  const initialSessionId = searchParams.get('sessionId') ?? '';

  const statCards = (
    <div className="mb-8 grid grid-cols-2 gap-4 lg:grid-cols-4">
      {[
        { label: 'Lecturer', value: c.lecturerName ?? '—' },
        { label: 'Enrolled', value: `${c.enrolledCount}/${c.maxCapacity}`, mono: true },
        { label: 'Start', value: c.startDate, mono: true },
        { label: 'End', value: c.endDate, mono: true },
      ].map((card) => (
        <div key={card.label} className="rounded-xl border border-border bg-white px-5 py-[18px]">
          <div className="mb-2 text-[12px] font-semibold uppercase tracking-wide text-text-muted">
            {card.label}
          </div>
          <div className={`text-[16px] font-semibold ${card.mono ? 'font-mono' : ''}`}>
            {card.value}
          </div>
        </div>
      ))}
    </div>
  );

  return (
    <div className="max-w-[1040px] p-5 sm:p-8 md:p-10 md:px-12">
      {/* Back */}
      <Button
        type="text"
        icon={<ArrowLeftOutlined />}
        onClick={() => navigate(backTo)}
        className="mb-4 p-0 text-text-secondary"
      >
        Back to courses
      </Button>

      {/* Header */}
      <div className="mb-6 flex items-start justify-between gap-4">
        <div>
          <div className="mb-2 flex flex-wrap items-center gap-3">
            <h1 className="m-0 text-[30px] font-bold tracking-tight">{c.name}</h1>
            <StatusTag status={c.status} />
          </div>
          {c.description && (
            <p className="m-0 max-w-[620px] text-[15px] leading-relaxed text-text-secondary">
              {c.description}
            </p>
          )}
        </div>
        {canManage && (
          <div className="flex flex-none gap-2">
            <Button onClick={() => setReplaceOpen(true)}>Replace lecturer</Button>
            <Button onClick={() => {
              // DatePicker cần Dayjs, không nhận string — convert endDate khi mở modal
              editForm.setFieldsValue({
                name: c.name,
                description: c.description,
                maxCapacity: c.maxCapacity,
                endDate: c.endDate ? dayjs(c.endDate) : undefined,
              });
              setEditOpen(true);
            }}>
              Edit course
            </Button>
            {/* Chỉ Upcoming mới xóa được (khớp ràng buộc backend) — ẩn nút ở trạng thái khác
                để tránh Admin bấm rồi nhận lỗi. Backend vẫn chặn kể cả khi gọi trực tiếp. */}
            {c.status === 'Upcoming' && (
              <Popconfirm
                title="Delete this course?"
                description="This permanently removes the course and all its sessions. Only allowed if no students are enrolled."
                okText="Delete"
                okButtonProps={{ danger: true, loading: deleteCourse.isPending }}
                onConfirm={() =>
                  deleteCourse.mutate(undefined, { onSuccess: () => navigate(backTo) })
                }
              >
                <Button danger>Delete course</Button>
              </Popconfirm>
            )}
          </div>
        )}
      </div>

      {canManage ? (
        <>
          {statCards}

          {/* Admin: lưới weekly pattern để thêm/xóa slot (cơ chế tạo ClassSession) */}
          <div className="mb-3.5 flex items-center justify-between">
            <h2 className="m-0 text-[20px] font-semibold tracking-tight">Class sessions</h2>
          </div>
          <WeeklySlotGrid
            slots={weeklySlots.data ?? []}
            onAdd={(dayOfWeek, sessionType) => addSlot.mutate({ dayOfWeek, sessionType })}
            onRemove={(slot) => removeSlot.mutate(slot.weeklySlotId)}
          />
          <p className="mt-3 text-[13px] text-text-muted">
            Click a day &amp; shift to edit the weekly pattern. Sessions repeat every week from start to end date.
          </p>

          {/* Session history: bảng chi tiết per-date (cancellations, past/upcoming) — chỉ Admin */}
          <div className="mb-3.5 mt-8 flex items-center justify-between">
            <h2 className="m-0 text-[20px] font-semibold tracking-tight">Session history</h2>
            <label className="flex cursor-pointer items-center gap-2 text-[13px] text-text-secondary">
              <Switch size="small" checked={hidePast} onChange={setHidePast} />
              Hide past sessions
            </label>
          </div>
          <Table<ClassSession>
            columns={sessionColumns}
            dataSource={hidePast ? (c.classSessions ?? []).filter((s) => !s.isPast) : c.classSessions}
            rowKey="classSessionId"
            pagination={{ pageSize: 10 }}
            rowClassName={(record) => (record.isCancelled ? 'line-through opacity-50' : '')}
          />
        </>
      ) : (
        // Lecturer: 3 tab — Detail (thông tin + lịch tuần), Attendance, Grading.
        // (Attendance/Grading là hoạt động của Lecturer; Admin không có 2 tab này.)
        <Tabs
          activeKey={activeTab}
          onChange={setActiveTab}
          items={[
            {
              key: 'detail',
              label: 'Detail',
              children: (
                <>
                  {statCards}
                  <div className="mb-3.5 flex items-center justify-between">
                    <h2 className="m-0 text-[20px] font-semibold tracking-tight">Class schedule</h2>
                  </div>
                  <CourseWeeklySchedule sessions={c.classSessions ?? []} />
                </>
              ),
            },
            {
              key: 'attendance',
              label: 'Attendance',
              children: <AttendancePanel courseId={courseId!} initialSessionId={initialSessionId} />,
            },
            {
              key: 'grading',
              label: 'Grading',
              children: <GradingPanel courseId={courseId!} courseStatus={c.status} />,
            },
          ]}
        />
      )}

      {/* Edit Course Modal */}
      <Modal
        title="Edit course"
        open={editOpen}
        onOk={() => editForm.validateFields().then((v) => updateCourse.mutate({
          ...v,
          endDate: typeof v.endDate === 'string' ? v.endDate : (v.endDate as unknown as { format: (f: string) => string }).format('YYYY-MM-DD'),
        }))}
        onCancel={() => setEditOpen(false)}
        confirmLoading={updateCourse.isPending}
        okText="Save"
        destroyOnHidden
      >
        <Form form={editForm} layout="vertical" requiredMark={false} className="mt-4">
          <Form.Item label="Course name" name="name" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item label="Description" name="description">
            <Input.TextArea rows={3} />
          </Form.Item>
          <div className="grid grid-cols-2 gap-3">
            <Form.Item label="End date" name="endDate" rules={[{ required: true }]}>
              <DatePicker className="w-full" />
            </Form.Item>
            <Form.Item label="Max capacity" name="maxCapacity" rules={[{ required: true }]}>
              <InputNumber min={1} className="w-full" />
            </Form.Item>
          </div>
        </Form>
      </Modal>

      {/* Replace Lecturer Modal */}
      <Modal
        title="Replace lecturer"
        open={replaceOpen}
        onOk={() => replaceForm.validateFields().then((v) => replaceLecturer.mutate(v))}
        onCancel={() => setReplaceOpen(false)}
        confirmLoading={replaceLecturer.isPending}
        okText="Replace"
        destroyOnHidden
      >
        <p className="mb-4 text-[14px] text-text-secondary">
          Currently assigned: <strong>{c.lecturerName}</strong>
        </p>
        <Form form={replaceForm} layout="vertical" requiredMark={false}>
          <Form.Item label="New lecturer" name="lecturerId" rules={[{ required: true, message: 'Select a new lecturer' }]}>
            <LecturerPicker />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
