import { useState } from 'react';
import { Modal, Select, Spin, Tag } from 'antd';
import { SwapOutlined, InfoCircleOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { getCourseWeeklySlotsApi } from './enrollment.api';
import type { MyCourseItem } from './enrollment.types';
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
  const session = slot.sessionType === 'Morning' ? 'Sáng (AM)' : 'Chiều (PM)';
  return `${DAY_VN[slot.dayOfWeek] ?? slot.dayOfWeek} — ${session}`;
}

const STATUS_COLOR: Record<string, string> = {
  Upcoming: '#E5A20B',
  Active: '#1E875F',
  Completed: '#8A847E',
};

interface Props {
  enrollment: MyCourseItem | null;
  onClose: () => void;
  onConfirm: (enrollmentId: string, oldWeeklySlotId: string, newWeeklySlotId: string) => void;
  isLoading: boolean;
}

export default function AdjustSessionModal({ enrollment, onClose, onConfirm, isLoading }: Props) {
  const [oldSlotId, setOldSlotId] = useState<string | undefined>();
  const [newSlotId, setNewSlotId] = useState<string | undefined>();

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
    }
  }

  const slots = slotsQuery.data ?? [];
  const canSubmit = !!oldSlotId && !!newSlotId && oldSlotId !== newSlotId;
  const statusColor = enrollment ? (STATUS_COLOR[enrollment.status] ?? '#8A847E') : '#8A847E';

  return (
    <Modal
      title={
        <div className="flex items-center gap-2">
          <SwapOutlined style={{ color: '#F45D48' }} />
          <span>Điều chỉnh ca học</span>
        </div>
      }
      open={!!enrollment}
      onOk={handleOk}
      onCancel={onClose}
      okText="Xác nhận đổi"
      cancelText="Hủy"
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
            <span>Mỗi lần chỉ đổi 1 slot. Đổi cả 2 slot thì thực hiện 2 lần.</span>
          </div>
        </div>
      )}

      {slotsQuery.isLoading ? (
        <div className="flex justify-center py-8">
          <Spin />
        </div>
      ) : slots.length === 0 ? (
        <div className="rounded-xl border border-border bg-bg-card px-4 py-6 text-center text-[14px] text-text-muted">
          Khóa học chưa có slot học nào.
        </div>
      ) : (
        <div className="flex flex-col gap-4">
          <div>
            <p className="mb-2 text-[13px] font-semibold uppercase tracking-wide text-text-muted">
              1 · Slot muốn thay thế
            </p>
            <Select
              placeholder="Chọn slot hiện tại muốn đổi"
              value={oldSlotId}
              onChange={(v) => {
                setOldSlotId(v);
                if (newSlotId === v) setNewSlotId(undefined);
              }}
              className="w-full"
              size="large"
              options={slots.map((s) => ({
                value: s.weeklySlotId,
                label: formatSlot(s),
              }))}
            />
          </div>

          <div
            className="flex items-center gap-3"
            style={{ opacity: oldSlotId ? 1 : 0.4, transition: 'opacity 0.2s' }}
          >
            <div className="h-px flex-1 bg-border" />
            <SwapOutlined style={{ color: '#F45D48', fontSize: 16 }} />
            <div className="h-px flex-1 bg-border" />
          </div>

          <div>
            <p className="mb-2 text-[13px] font-semibold uppercase tracking-wide text-text-muted">
              2 · Slot mới muốn chuyển sang
            </p>
            <Select
              placeholder={oldSlotId ? 'Chọn slot mới' : 'Chọn slot cũ trước'}
              value={newSlotId}
              onChange={setNewSlotId}
              className="w-full"
              size="large"
              disabled={!oldSlotId}
              options={slots
                .filter((s) => s.weeklySlotId !== oldSlotId)
                .map((s) => ({
                  value: s.weeklySlotId,
                  label: formatSlot(s),
                }))}
            />
          </div>
        </div>
      )}
    </Modal>
  );
}
