import type { SessionType } from './schedule.types';

// Sắp buổi trong cùng 1 ngày: Morning (AM) lên trên, Afternoon (PM) xuống dưới.
// Dùng chung cho lịch Lecturer lẫn Student để thứ tự hiển thị nhất quán.
export function sortSessionsByShift<T extends { sessionType: SessionType }>(items: T[]): T[] {
  return items.sort((a, b) => {
    if (a.sessionType === b.sessionType) return 0;
    return a.sessionType === 'Morning' ? -1 : 1;
  });
}
