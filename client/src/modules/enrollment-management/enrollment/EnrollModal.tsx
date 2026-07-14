import { useState } from 'react';
import { Modal, Spin, Alert } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { getCourseWeeklySlotsApi } from './enrollment.api';
import SessionPicker from './SessionPicker';
import type { AvailableCourse } from './enrollment.types';
import type { WeeklySlot } from '@/modules/course-management/shared/course.types';

function formatSlot(slot: WeeklySlot) {
  return `${slot.dayOfWeek} — ${slot.sessionType === 'Morning' ? 'Morning (AM)' : 'Afternoon (PM)'}`;
}

function capacityColor(pct: number) {
  if (pct >= 1) return '#D7372C';
  if (pct >= 0.75) return '#E5A20B';
  return '#1E875F';
}

interface Props {
  course: AvailableCourse | null;
  onClose: () => void;
  onConfirm: (courseId: string, weeklySlotIds: string[]) => void;
  isLoading: boolean;
}

export default function EnrollModal({ course, onClose, onConfirm, isLoading }: Props) {
  const [slotIds, setSlotIds] = useState<[string | undefined, string | undefined]>([
    undefined,
    undefined,
  ]);
  const [pickerIndex, setPickerIndex] = useState<0 | 1 | null>(null);

  const slotsQuery = useQuery({
    queryKey: ['weekly-slots', course?.courseId],
    queryFn: () => getCourseWeeklySlotsApi(course!.courseId),
    select: (res) => res.data,
    enabled: !!course,
  });

  function handleOk() {
    if (!course || !slotIds[0] || !slotIds[1]) return;
    onConfirm(course.courseId, [slotIds[0], slotIds[1]]);
  }

  // reset khi modal đóng/mở course mới
  function afterOpenChange(open: boolean) {
    if (!open) {
      setSlotIds([undefined, undefined]);
      setPickerIndex(null);
    }
  }

  const slots = slotsQuery.data ?? [];
  const isExactTwo = !!slotIds[0] && !!slotIds[1];
  const pct = course && course.maxCapacity > 0 ? course.enrolledCount / course.maxCapacity : 0;

  return (
    <Modal
      title={`Enroll: ${course?.name ?? ''}`}
      open={!!course}
      onOk={handleOk}
      onCancel={onClose}
      okText="Enroll"
      okButtonProps={{ disabled: !isExactTwo, loading: isLoading }}
      destroyOnHidden
      afterOpenChange={afterOpenChange}
    >
      {course && (
        <div className="mb-4 rounded-xl p-3.5" style={{ background: '#FBEDE4' }}>
          <div className="mb-1.5 flex justify-between text-[13px]">
            <span className="font-semibold text-text-secondary">Capacity</span>
            <span className="font-mono font-semibold">
              {course.enrolledCount}/{course.maxCapacity}
            </span>
          </div>
          <div className="h-2 overflow-hidden rounded-full" style={{ background: '#F1E7DD' }}>
            <div
              className="h-full rounded-full"
              style={{ width: `${Math.min(pct, 1) * 100}%`, background: capacityColor(pct) }}
            />
          </div>
        </div>
      )}

      {slotsQuery.isLoading ? (
        <div className="flex justify-center py-6">
          <Spin />
        </div>
      ) : slots.length < 2 ? (
        // Rule: mỗi Enrollment cần chọn đúng 2 WeeklySlot. Course chưa đủ 2 slot (Admin
        // chưa thêm/ mới thêm 1) thì KHÔNG thể đăng ký — báo rõ thay vì để nút disable
        // im lặng khiến Student không hiểu vì sao.
        <Alert
          type="warning"
          showIcon
          title="Not open for enrollment yet"
          description="This course doesn't have enough sessions scheduled yet (at least 2 are required). Please check back later."
        />
      ) : (
        <>
          <label className="mb-2.5 block text-[14px] font-semibold">Choose exactly 2 sessions</label>

          {pickerIndex !== null ? (
            <SessionPicker
              slots={slots}
              disabledSlotIds={[slotIds[1 - pickerIndex]].filter((v): v is string => !!v)}
              selectedSlotId={slotIds[pickerIndex]}
              onPick={(id) => {
                const next: [string | undefined, string | undefined] = [...slotIds];
                next[pickerIndex] = id;
                setSlotIds(next);
                setPickerIndex(null);
              }}
            />
          ) : (
            <div className="flex flex-col gap-2.5">
              {([0, 1] as const).map((i) => {
                const slot = slots.find((s) => s.weeklySlotId === slotIds[i]);
                return (
                  <button
                    key={i}
                    type="button"
                    onClick={() => setPickerIndex(i)}
                    className="flex h-12 w-full items-center justify-between rounded-lg border border-border-input bg-white px-3.5 text-[15px] font-semibold"
                    style={{ color: slot ? '#1C1B1A' : '#8A847E' }}
                  >
                    <span>{slot ? formatSlot(slot) : `Select slot ${i + 1}`}</span>
                    <span className="text-[13px] font-medium text-text-muted">Change</span>
                  </button>
                );
              })}
            </div>
          )}

          <div className="mt-3 text-[13px] text-text-muted">
            {!isExactTwo && (slotIds[0] || slotIds[1])
              ? 'Selected 1/2 slots. Select 1 more slot to finish.'
              : 'The selected slots must be different and must not conflict with your other courses.'}
          </div>
        </>
      )}
    </Modal>
  );
}
