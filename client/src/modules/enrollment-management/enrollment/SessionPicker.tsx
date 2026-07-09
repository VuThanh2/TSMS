import type { WeeklySlot } from '@/modules/course-management/shared/course.types';

// Grid weekday x ca (AM/PM) để chọn 1 WeeklySlot — dùng chung cho EnrollModal
// (chọn 2 slot) và AdjustSessionModal (đổi slot cũ -> mới). Chỉ trong nội bộ
// module enrollment nên đặt cùng thư mục, không đẩy lên shared/ (Rule of Three).
const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
const DAY_SHORT = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];

interface SessionPickerProps {
  slots: WeeklySlot[];
  disabledSlotIds?: string[];
  selectedSlotId?: string;
  onPick: (weeklySlotId: string) => void;
}

export default function SessionPicker({
  slots,
  disabledSlotIds = [],
  selectedSlotId,
  onPick,
}: SessionPickerProps) {
  function findSlot(day: string, type: string) {
    return slots.find((s) => s.dayOfWeek === day && s.sessionType === type);
  }

  function renderRow(type: string) {
    return DAYS.map((day) => {
      const slot = findSlot(day, type);
      if (!slot) return <div key={day} className="h-9" />;
      const disabled = disabledSlotIds.includes(slot.weeklySlotId);
      const selected = slot.weeklySlotId === selectedSlotId;
      return (
        <button
          key={day}
          type="button"
          disabled={disabled}
          onClick={() => onPick(slot.weeklySlotId)}
          className={`h-9 rounded-lg border text-[12px] font-semibold transition-colors ${
            disabled
              ? 'cursor-not-allowed border-border bg-bg-card text-text-muted opacity-50'
              : selected
                ? 'border-primary bg-primary text-white'
                : 'border-border-input bg-white text-text-secondary hover:border-primary hover:text-primary'
          }`}
        >
          {type === 'Morning' ? 'AM' : 'PM'}
        </button>
      );
    });
  }

  return (
    <div className="flex flex-col gap-3.5">
      <div className="text-[13px] text-text-muted">
        Choose a weekday and shift from this course&apos;s schedule.
      </div>
      <div
        className="grid items-center gap-1.5"
        style={{ gridTemplateColumns: '36px repeat(7, 1fr)' }}
      >
        <div />
        {DAY_SHORT.map((d) => (
          <div key={d} className="text-center text-[12px] font-bold text-text-muted">
            {d}
          </div>
        ))}
        <div className="text-[11px] font-bold text-text-muted">AM</div>
        {renderRow('Morning')}
        <div className="text-[11px] font-bold text-text-muted">PM</div>
        {renderRow('Afternoon')}
      </div>
    </div>
  );
}
