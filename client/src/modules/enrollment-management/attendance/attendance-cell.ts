import type { SessionAttendanceSummary } from './attendance.types';

// Nội dung ô lưới lịch cho buổi ĐÃ QUA — thay nhãn "Past" chung chung bằng số có mặt.
// Chỉ dùng cho state 'past': buổi cancelled/today/upcoming giữ nhãn trạng thái như cũ.
//
// Chưa điểm danh hiện "–/15" chứ KHÔNG phải "0/15": số 0 trông như số liệu thật và sẽ bị
// đọc thành "cả lớp nghỉ", trong khi sự thật là chưa ai chấm. Dấu – nói thẳng "chưa biết",
// và vẫn là lời nhắc Lecturer vào chấm.
export function formatAttendanceCell(summary: SessionAttendanceSummary): {
  label: string;
  title: string;
} {
  const { presentCount, excusedCount, absentCount, totalCount, isMarked } = summary;

  if (!isMarked) {
    return {
      label: `–/${totalCount}`,
      title: `Not marked yet · ${totalCount} student${totalCount === 1 ? '' : 's'} enrolled`,
    };
  }

  return {
    label: `${presentCount}/${totalCount}`,
    title: `${presentCount} present · ${excusedCount} excused · ${absentCount} absent`,
  };
}
