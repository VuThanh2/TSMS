import React, { useState } from 'react';
import { Calendar, Button } from 'antd';
import { LeftOutlined, RightOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';

// Calendar dùng chung cho lịch Lecturer/Student — điều hướng theo từng tháng
// (prev/next) thay vì hiển thị toàn bộ kỳ. Nhận sẵn map ngày -> danh sách buổi
// và hàm render chip để mỗi trang tự quyết định hiển thị buổi học thế nào.
interface ScheduleCalendarProps<T> {
  sessionsByDate: Record<string, T[]>;
  getKey: (item: T) => string;
  renderChip: (item: T) => React.ReactNode;
}

export default function ScheduleCalendar<T>({
  sessionsByDate,
  getKey,
  renderChip,
}: ScheduleCalendarProps<T>) {
  // Tháng đang xem — controlled để header prev/next điều khiển được
  const [value, setValue] = useState<Dayjs>(dayjs());

  // Chặn chọn ngày thuộc tháng trước/sau — đây là nguyên nhân Calendar tự đổi
  // tháng khi bấm vào ô đầu/cuối lưới (antd Calendar không tôn trọng hoàn toàn
  // controlled value khi cell được select, chỉ disabledDate mới chặn được click).
  function disabledDate(current: Dayjs) {
    return !current.isSame(value, 'month');
  }

  function cellRender(
    current: Dayjs,
    info: { type: string; originNode: React.ReactElement },
  ) {
    if (info.type !== 'date') return info.originNode;

    // Ô thuộc tháng trước/sau — để trống, không hiện số ngày, cho dễ nhìn
    if (!current.isSame(value, 'month')) return <div />;

    const dateStr = current.format('YYYY-MM-DD');
    const daySessions = sessionsByDate[dateStr] ?? [];
    if (daySessions.length === 0) return info.originNode;

    return (
      <>
        {info.originNode}
        <ul className="m-0 list-none p-0 pb-0.5">
          {daySessions.map((s) => (
            <li key={getKey(s)}>{renderChip(s)}</li>
          ))}
        </ul>
      </>
    );
  }

  return (
    <div className="overflow-hidden rounded-xl border border-border bg-white shadow-sm">
      <Calendar
        value={value}
        onChange={setValue}
        disabledDate={disabledDate}
        cellRender={cellRender}
        // Header tự vẽ: chỉ điều hướng theo tháng, ẩn dropdown năm/tháng mặc định
        headerRender={({ value: v }) => (
          <div className="flex items-center justify-between border-b border-border px-5 py-3.5">
            <div className="text-[18px] font-bold capitalize tracking-tight">
              {v.format('MMMM YYYY')}
            </div>
            <div className="flex items-center gap-2">
              <Button
                icon={<LeftOutlined />}
                onClick={() => setValue(value.clone().subtract(1, 'month'))}
                aria-label="Previous month"
              />
              <Button onClick={() => setValue(dayjs())}>Today</Button>
              <Button
                icon={<RightOutlined />}
                onClick={() => setValue(value.clone().add(1, 'month'))}
                aria-label="Next month"
              />
            </div>
          </div>
        )}
      />
    </div>
  );
}
