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
  useCancelClassSession,
  useOpenCourseEnrollment,
} from './useCourseDetail';

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
const DAY_SHORT = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const SESSION_TYPES = ['Morning', 'Afternoon'];

// Lưới weekly pattern chỉ dùng cho Admin (thêm/xóa slot). Lecturer xem lịch thật
// qua CourseWeeklySchedule nên không dùng grid này.
// readOnly: Course đã Completed — domain khóa mọi mutation (CompletedCourseIsImmutable),
// nên chỉ hiển thị pattern, không cho thêm/xóa.
function WeeklySlotGrid({
  slots,
  readOnly,
  onAdd,
  onRemove,
}: {
  slots: WeeklySlot[];
  readOnly: boolean;
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

              // Read-only: ô giữ nguyên hình dáng để đọc được pattern, nhưng bỏ hẳn
              // Popconfirm/onClick — không có affordance bấm thì user không kỳ vọng bấm được.
              if (readOnly) {
                return (
                  <div
                    key={`${day}-${type}`}
                    className={
                      slot
                        ? 'flex h-10 items-center justify-center rounded-lg bg-primary/50 text-[13px] font-semibold text-white'
                        : 'flex h-10 items-center justify-center rounded-lg border border-dashed border-border-input bg-transparent'
                    }
                  >
                    {slot ? '✓' : ''}
                  </div>
                );
              }

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
  const [activeTab, setActiveTab] = useState(() => {
    const t = searchParams.get('tab');
    return t === 'attendance' || t === 'grading' ? t : 'detail';
  });
  const [editForm] = Form.useForm();
  const [replaceForm] = Form.useForm();

  const updateCourse = useUpdateCourse(courseId!, () => setEditOpen(false));
  const replaceLecturer = useReplaceLecturer(courseId!, () => setReplaceOpen(false));
  const addSlot = useAddWeeklySlot(courseId!);
  const removeSlot = useRemoveWeeklySlot(courseId!);
  const deleteCourse = useDeleteCourse(courseId!);
  const cancelSession = useCancelClassSession(courseId!);
  const openEnrollment = useOpenCourseEnrollment(courseId!);

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

  // Course Completed = bất biến: domain chặn UpdateInfo/ReplaceLecturer/AddWeeklySlot/
  // RemoveWeeklySlot/CancelClassSession bằng CompletedCourseIsImmutable. Ẩn nút thay vì
  // để Admin bấm rồi nhận lỗi. Vẫn vào xem được thông tin & lịch sử buổi học.
  const isCompleted = c.status === 'Completed';

  // Cổng đăng ký. Chưa mở = Student không thấy course ⇒ Admin có cửa sổ an toàn để sửa/xóa.
  // Mở được khi: còn Upcoming + đã đủ 2 WeeklySlot (khớp invariant Course.OpenEnrollment).
  const slotCount = weeklySlots.data?.length ?? 0;
  const canOpenEnrollment = c.status === 'Upcoming' && !c.isOpenForEnrollment && slotCount >= 2;

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
            {/* Cổng đăng ký là trục riêng, KHÔNG gộp vào StatusTag — Status do job nền lái theo
                ngày, cổng do Admin bấm. Chỉ Admin thấy: Lecturer không kiểm soát việc này. */}
            {canManage && c.status === 'Upcoming' && (
              <Tag color={c.isOpenForEnrollment ? 'green' : 'default'}>
                {c.isOpenForEnrollment ? 'Enrollment open' : 'Not open for enrollment'}
              </Tag>
            )}
          </div>
          {c.description && (
            <p className="m-0 max-w-[620px] text-[15px] leading-relaxed text-text-secondary">
              {c.description}
            </p>
          )}
        </div>
        {canManage && !isCompleted && (
          <div className="flex flex-none gap-2">
            {/* Chỉ hiện khi thật sự mở được (Upcoming + đủ 2 slot) — thiếu slot thì banner
                bên dưới hướng dẫn, không để nút bấm được rồi báo lỗi. */}
            {canOpenEnrollment && (
              <Popconfirm
                title="Open this course for enrollment?"
                description="Students will be able to enroll from now on. Once someone enrolls, you can no longer delete this course."
                okText="Open enrollment"
                okButtonProps={{ loading: openEnrollment.isPending }}
                onConfirm={() => openEnrollment.mutate()}
              >
                <Button type="primary">Open enrollment</Button>
              </Popconfirm>
            )}
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
            {/* Backend cho xóa khi: Upcoming (Course.Delete) VÀ chưa ai enroll (DeleteCourseHandler).
                Ẩn nút ở mọi trường hợp khác thay vì để Admin bấm rồi nhận lỗi — có Student rồi
                thì xóa là hủy luôn lịch/điểm danh của họ, đó là lý do backend chặn hẳn.
                Backend vẫn chặn kể cả khi gọi trực tiếp. */}
            {c.status === 'Upcoming' && c.enrolledCount === 0 && (
              <Popconfirm
                title="Delete this course?"
                description="This permanently removes the course and all its sessions."
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
          {/* Không có banner thì Admin sẽ tưởng UI hỏng khi thấy course này thiếu nút so với course khác */}
          {isCompleted && (
            <div className="mb-6 rounded-lg border border-border bg-bg-card px-4 py-3 text-[14px] text-text-secondary">
              This course is <strong>Completed</strong> and can no longer be modified. Details and
              session history stay available to view.
            </div>
          )}

          {/* Giai đoạn dựng course: nói rõ Student chưa thấy gì, và còn thiếu bước nào để mở. */}
          {c.status === 'Upcoming' && !c.isOpenForEnrollment && (
            <div className="mb-6 rounded-lg border border-border bg-bg-card px-4 py-3 text-[14px] text-text-secondary">
              Students can&apos;t see this course yet.{' '}
              {slotCount < 2
                ? 'Add at least 2 weekly slots below, then open it for enrollment.'
                : 'Review the schedule, then use Open enrollment when you’re ready.'}{' '}
              You can still edit or delete it freely until someone enrolls.
            </div>
          )}

          {/* Đã mở + đã có Student: giải thích vì sao nút Delete biến mất. */}
          {c.status === 'Upcoming' && c.isOpenForEnrollment && c.enrolledCount > 0 && (
            <div className="mb-6 rounded-lg border border-border bg-bg-card px-4 py-3 text-[14px] text-text-secondary">
              <strong>{c.enrolledCount}</strong> student{c.enrolledCount !== 1 ? 's have' : ' has'}{' '}
              enrolled, so this course can no longer be deleted. You can still edit its details.
            </div>
          )}

          {statCards}

          {/* Admin: lưới weekly pattern để thêm/xóa slot (cơ chế tạo ClassSession) */}
          <div className="mb-3.5 flex items-center justify-between">
            <h2 className="m-0 text-[20px] font-semibold tracking-tight">Class sessions</h2>
          </div>
          <WeeklySlotGrid
            slots={weeklySlots.data ?? []}
            readOnly={isCompleted}
            onAdd={(dayOfWeek, sessionType) => addSlot.mutate({ dayOfWeek, sessionType })}
            onRemove={(slot) => removeSlot.mutate(slot.weeklySlotId)}
          />
          <p className="mt-3 text-[13px] text-text-muted">
            {isCompleted
              ? 'The weekly pattern this course ran on. Completed courses are read-only.'
              : 'Click a day & shift to edit the weekly pattern. Sessions repeat every week from start to end date.'}
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
            columns={[
              ...sessionColumns,
              // Course Completed → bỏ hẳn cột action (domain chặn CancelClassSession).
              // Trước đây cột này tự rỗng vì mọi buổi đều isPast, nhưng đó là trùng hợp
              // chứ không phải chủ đích — bỏ cột để bảng đọc gọn hơn.
              ...(isCompleted
                ? []
                : [
                    {
                      title: '',
                      key: 'actions',
                      width: 100,
                      align: 'right' as const,
                      // Chỉ buổi sắp diễn ra & chưa hủy mới cancel được (khớp ràng buộc domain:
                      // past → CannotModifyPastClassSession, đã hủy → AlreadyCancelled). Buổi đã qua
                      // hoặc đã hủy không hiện nút để tránh Admin bấm rồi nhận lỗi.
                      render: (_: unknown, record: ClassSession) =>
                        record.isPast || record.isCancelled ? null : (
                          <Popconfirm
                            title="Cancel this session?"
                            description="Students will see this session as cancelled. This cannot be undone."
                            okText="Cancel session"
                            okButtonProps={{ danger: true, loading: cancelSession.isPending }}
                            onConfirm={() => cancelSession.mutate(record.classSessionId)}
                          >
                            <Button danger>Cancel</Button>
                          </Popconfirm>
                        ),
                    },
                  ]),
            ]}
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
                  <CourseWeeklySchedule sessions={c.classSessions ?? []} courseId={courseId!} />
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
          {/* name PHẢI là newLecturerId: giá trị form đi thẳng vào body request, và BE bind theo
              đúng tên này (ReplaceLecturerInputDto.NewLecturerId). */}
          <Form.Item label="New lecturer" name="newLecturerId" rules={[{ required: true, message: 'Select a new lecturer' }]}>
            <LecturerPicker excludeId={c.lecturerId} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
