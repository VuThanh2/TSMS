import dayjs from 'dayjs';
import type { ClassSession } from './course.types';

// Trạng thái 1 buổi học — thông tin có ý nghĩa nhất để hiển thị trong lưới lịch tuần
// (hàng đã là AM/PM, cột đã là ngày, nên nội dung ô nên nói lên TRẠNG THÁI buổi học).
export type SessionState = 'cancelled' | 'today' | 'past' | 'upcoming';

export function getSessionState(
  s: Pick<ClassSession, 'isCancelled' | 'isPast' | 'sessionDate'>,
): SessionState {
  if (s.isCancelled) return 'cancelled';
  // Ưu tiên "today" hơn "past" để buổi diễn ra hôm nay luôn nổi bật (kể cả ca sáng đã qua).
  if (dayjs(s.sessionDate).isSame(dayjs(), 'day')) return 'today';
  if (s.isPast) return 'past';
  return 'upcoming';
}

export const SESSION_STATE_LABEL: Record<SessionState, string> = {
  cancelled: 'Cancelled',
  today: 'Today',
  past: 'Past',
  upcoming: 'Upcoming',
};

// Màu theo trạng thái (nền nhạt + chữ) — dùng thống nhất giữa các lưới lịch tuần.
export const SESSION_STATE_STYLE: Record<SessionState, { bg: string; color: string }> = {
  upcoming: { bg: '#EAF6F0', color: '#1E875F' }, // xanh lá — sắp diễn ra
  today: { bg: 'rgba(244, 93, 72, 0.16)', color: '#F45D48' }, // primary — hôm nay
  past: { bg: '#F1EEEB', color: '#8A847E' }, // xám — đã qua
  cancelled: { bg: '#EFECE8', color: '#9A9691' }, // xám + gạch ngang — đã hủy
};
