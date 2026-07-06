import { useState } from 'react';
import { Modal, Checkbox, Spin, Alert } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { getCourseWeeklySlotsApi } from './enrollment.api';
import type { AvailableCourse } from './enrollment.types';
import type { WeeklySlot } from '@/modules/course-management/shared/course.types';

const DAY_VN: Record<string, string> = {
  Monday: 'Thứ Hai',
  Tuesday: 'Thứ Ba',
  Wednesday: 'Thứ Tư',
  Thursday: 'Thứ Năm',
  Friday: 'Thứ Sáu',
  Saturday: 'Thứ Bảy',
  Sunday: 'Chủ Nhật',
};

function formatSlot(slot: WeeklySlot) {
  return `${DAY_VN[slot.dayOfWeek] ?? slot.dayOfWeek} — ${slot.sessionType === 'Morning' ? 'Sáng (AM)' : 'Chiều (PM)'}`;
}

interface Props {
  course: AvailableCourse | null;
  onClose: () => void;
  onConfirm: (courseId: string, weeklySlotIds: string[]) => void;
  isLoading: boolean;
}

export default function EnrollModal({ course, onClose, onConfirm, isLoading }: Props) {
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const slotsQuery = useQuery({
    queryKey: ['weekly-slots', course?.courseId],
    queryFn: () => getCourseWeeklySlotsApi(course!.courseId),
    select: (res) => res.data,
    enabled: !!course,
  });

  function handleOk() {
    if (!course) return;
    onConfirm(course.courseId, selectedIds);
  }

  // reset khi modal đóng/mở course mới
  function afterOpenChange(open: boolean) {
    if (!open) setSelectedIds([]);
  }

  const slots = slotsQuery.data ?? [];
  const isExactTwo = selectedIds.length === 2;

  return (
    <Modal
      title={`Đăng ký: ${course?.name ?? ''}`}
      open={!!course}
      onOk={handleOk}
      onCancel={onClose}
      okText="Đăng ký"
      okButtonProps={{ disabled: !isExactTwo, loading: isLoading }}
      destroyOnHidden
      afterOpenChange={afterOpenChange}
    >
      {/* Course info */}
      {course && (
        <div className="mb-4 grid grid-cols-2 gap-2 rounded-lg border border-border bg-bg-card px-4 py-3 text-[14px]">
          <span className="text-text-muted">Giảng viên</span>
          <span className="font-semibold">{course.lecturerName}</span>
          <span className="text-text-muted">Thời gian</span>
          <span className="font-mono">
            {course.startDate} – {course.endDate}
          </span>
          <span className="text-text-muted">Còn chỗ</span>
          <span className="font-mono font-semibold">
            {course.maxCapacity - course.enrolledCount}/{course.maxCapacity}
          </span>
        </div>
      )}

      <p className="mb-3 text-[14px] text-text-secondary">
        Chọn đúng <strong>2 slot học</strong> để hoàn tất đăng ký.
      </p>

      {slotsQuery.isLoading ? (
        <div className="flex justify-center py-6">
          <Spin />
        </div>
      ) : slots.length === 0 ? (
        <Alert type="warning" description="Khóa học chưa có slot học nào." showIcon />
      ) : (
        <Checkbox.Group
          value={selectedIds}
          onChange={(vals) => setSelectedIds(vals as string[])}
          className="flex w-full flex-col gap-2"
        >
          {slots.map((slot) => (
            <Checkbox
              key={slot.weeklySlotId}
              value={slot.weeklySlotId}
              disabled={selectedIds.length >= 2 && !selectedIds.includes(slot.weeklySlotId)}
              className="rounded-lg border border-border px-4 py-2.5 hover:bg-bg-card"
            >
              {formatSlot(slot)}
            </Checkbox>
          ))}
        </Checkbox.Group>
      )}

      {!isExactTwo && selectedIds.length > 0 && (
        <p className="mt-3 text-[13px] text-[#D7372C]">
          Đã chọn {selectedIds.length}/2 slot.
        </p>
      )}
    </Modal>
  );
}
