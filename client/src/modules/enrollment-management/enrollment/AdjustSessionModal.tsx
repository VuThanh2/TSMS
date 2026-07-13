import { useState } from 'react';
import { Modal, Spin, Tag } from 'antd';
import { SwapOutlined, InfoCircleOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { getCourseWeeklySlotsApi } from './enrollment.api';
import SessionPicker from './SessionPicker';
import type { MyCourseItem } from './enrollment.types';
import type { WeeklySlot } from '@/modules/course-management/shared/course.types';

function formatSlot(slot: WeeklySlot) {
  const session = slot.sessionType === 'Morning' ? 'Morning (AM)' : 'Afternoon (PM)';
  return `${slot.dayOfWeek} — ${session}`;
}

const STATUS_COLOR: Record<string, string> = {
  Upcoming: '#E5A20B',
  Active: '#1E875F',
  Completed: '#8A847E',
};

type PickerTarget = 'from' | 'to' | null;

interface Props {
  enrollment: MyCourseItem | null;
  onClose: () => void;
  onConfirm: (enrollmentId: string, oldWeeklySlotId: string, newWeeklySlotId: string) => void;
  isLoading: boolean;
}

export default function AdjustSessionModal({ enrollment, onClose, onConfirm, isLoading }: Props) {
  const [oldSlotId, setOldSlotId] = useState<string | undefined>();
  const [newSlotId, setNewSlotId] = useState<string | undefined>();
  const [picker, setPicker] = useState<PickerTarget>(null);

  const slotsQuery = useQuery({
    queryKey: ['weekly-slots', enrollment?.courseId],
    queryFn: () => getCourseWeeklySlotsApi(enrollment!.courseId),
    select: (res) => res.data,
    enabled: !!enrollment,
  });

  function handleOk() {
    if (!enrollment || !oldSlotId || !newSlotId) return;
    onConfirm(enrollment.enrollmentId, oldSlotId, newSlotId);
  }

  function afterOpenChange(open: boolean) {
    if (!open) {
      setOldSlotId(undefined);
      setNewSlotId(undefined);
      setPicker(null);
    }
  }

  const slots = slotsQuery.data ?? [];
  const enrolledIds = enrollment?.enrolledWeeklySlotIds ?? [];
  const currentSlots = slots.filter((s) => enrolledIds.includes(s.weeklySlotId));
  const canSubmit = !!oldSlotId && !!newSlotId && oldSlotId !== newSlotId;
  const statusColor = enrollment ? (STATUS_COLOR[enrollment.status] ?? '#8A847E') : '#8A847E';
  const oldSlot = slots.find((s) => s.weeklySlotId === oldSlotId);
  const newSlot = slots.find((s) => s.weeklySlotId === newSlotId);

  return (
    <Modal
      title={
        <div className="flex items-center gap-2">
          <SwapOutlined style={{ color: '#F45D48' }} />
          <span>Adjust session</span>
        </div>
      }
      open={!!enrollment}
      onOk={handleOk}
      onCancel={onClose}
      okText="Confirm change"
      cancelText="Cancel"
      okButtonProps={{
        disabled: !canSubmit,
        loading: isLoading,
        style: canSubmit ? { background: '#F45D48', borderColor: '#F45D48' } : undefined,
      }}
      destroyOnHidden
      afterOpenChange={afterOpenChange}
      styles={{ body: { paddingTop: 8 } }}
    >
      {/* Course info card */}
      {enrollment && (
        <div className="mb-5 rounded-xl border border-border bg-bg-card px-4 py-3">
          <div className="mb-1 flex items-center justify-between">
            <span className="text-[15px] font-bold">{enrollment.courseName}</span>
            <Tag
              style={{
                background: statusColor + '1A',
                color: statusColor,
                border: `1px solid ${statusColor}33`,
                borderRadius: 6,
                fontWeight: 600,
                fontSize: 12,
              }}
            >
              {enrollment.status}
            </Tag>
          </div>
          <div className="flex items-center gap-1.5 text-[12px] text-text-muted">
            <InfoCircleOutlined />
            <span>You can only change one slot at a time. To change both slots, do it twice.</span>
          </div>
        </div>
      )}

      {slotsQuery.isLoading ? (
        <div className="flex justify-center py-8">
          <Spin />
        </div>
      ) : slots.length === 0 ? (
        <div className="rounded-xl border border-border bg-bg-card px-4 py-6 text-center text-[14px] text-text-muted">
          This course has no slots yet.
        </div>
      ) : picker !== null ? (
        <SessionPicker
          slots={picker === 'from' ? currentSlots : slots}
          disabledSlotIds={[picker === 'from' ? newSlotId : oldSlotId].filter(
            (v): v is string => !!v,
          )}
          selectedSlotId={picker === 'from' ? oldSlotId : newSlotId}
          onPick={(id) => {
            if (picker === 'from') {
              setOldSlotId(id);
              if (newSlotId === id) setNewSlotId(undefined);
            } else {
              setNewSlotId(id);
            }
            setPicker(null);
          }}
        />
      ) : (
        <div className="flex flex-col gap-[18px]">
          <div>
            <label className="mb-2.5 block text-[14px] font-semibold">
              Current session to change
            </label>
            <button
              type="button"
              onClick={() => setPicker('from')}
              className="flex h-12 w-full items-center justify-between rounded-lg border border-border-input bg-white px-3.5 text-[15px] font-semibold"
              style={{ color: oldSlot ? '#1C1B1A' : '#8A847E' }}
            >
              <span>{oldSlot ? formatSlot(oldSlot) : 'Select the slot to change'}</span>
              <span className="text-[13px] font-medium text-text-muted">Change</span>
            </button>
          </div>

          <div>
            <label className="mb-2.5 block text-[14px] font-semibold">Switch to</label>
            <button
              type="button"
              disabled={!oldSlotId}
              onClick={() => setPicker('to')}
              className="flex h-12 w-full items-center justify-between rounded-lg border border-border-input bg-white px-3.5 text-[15px] font-semibold disabled:cursor-not-allowed disabled:opacity-50"
              style={{ color: newSlot ? '#1C1B1A' : '#8A847E' }}
            >
              <span>{newSlot ? formatSlot(newSlot) : oldSlotId ? 'Select the new slot' : 'Select the old slot first'}</span>
              <span className="text-[13px] font-medium text-text-muted">Change</span>
            </button>
          </div>
        </div>
      )}
    </Modal>
  );
}
